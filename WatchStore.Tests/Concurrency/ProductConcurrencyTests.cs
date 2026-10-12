using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Exceptions;
using WatchStore.Data.Context;
using WatchStore.Data.Repositories;

namespace WatchStore.Tests.Concurrency
{
    // ההוכחה שה-concurrency token עובד באמת: שני DbContext נפרדים (כמו שתי בקשות HTTP
    // מקבילות) קוראים את אותו מוצר, שניהם משנים אותו, הראשון שומר והשני נכשל.
    // רץ מול SQL Server LocalDB אמיתי, כי rowversion לא קיים ב-InMemory provider.
    // כל הרצה יוצרת בסיס נתונים זמני משלה ומוחקת אותו בסוף
    public class ProductConcurrencyTests : IAsyncLifetime
    {
        private const int RolexId = 1; // ב-seed יש ממנו יחידה אחת בלבד

        private readonly string _connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database=WatchStore_ConcurrencyTest_{Guid.NewGuid():N};" +
            "Trusted_Connection=True;TrustServerCertificate=True;";

        public async Task InitializeAsync()
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }

        [Fact]
        public async Task TwoContextsUpdateSameProduct_FirstSaveWins_SecondThrowsDbUpdateConcurrencyException()
        {
            await using var storeA = CreateContext();
            await using var storeB = CreateContext();

            // שתי ה"בקשות" קוראות את המוצר כשהמלאי עדיין 1
            var productA = await storeA.Products.SingleAsync(p => p.Id == RolexId);
            var productB = await storeB.Products.SingleAsync(p => p.Id == RolexId);

            Assert.Equal(1, productA.StockQuantity);
            Assert.Equal(1, productB.StockQuantity);

            // שתיהן עברו את בדיקת המלאי ומורידות את היחידה האחרונה
            productA.StockQuantity -= 1;
            productB.StockQuantity -= 1;

            // הראשונה שומרת בהצלחה, וה-RowVersion בשורה משתנה
            await storeA.SaveChangesAsync();

            // השנייה שולחת UPDATE ... WHERE RowVersion = <הערך הישן> - אף שורה לא מתעדכנת
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => storeB.SaveChangesAsync());

            // ובבסיס הנתונים: המלאי ירד פעם אחת בלבד, לא ל-1-
            await using var verify = CreateContext();
            var final = await verify.Products.AsNoTracking().SingleAsync(p => p.Id == RolexId);
            Assert.Equal(0, final.StockQuantity);
        }

        [Fact]
        public async Task RepositoryTranslatesConflictToCoreException()
        {
            await using var storeA = CreateContext();
            await using var storeB = CreateContext();

            var repoA = new ProductRepository(storeA);
            var repoB = new ProductRepository(storeB);

            var productA = await repoA.GetByIdAsync(RolexId);
            var productB = await repoB.GetByIdAsync(RolexId);

            productA!.StockQuantity -= 1;
            productB!.StockQuantity -= 1;

            await repoA.SaveChangesAsync();

            // ה-Service מקבל חריגה של ה-Core ולא של EF, ומתרגם אותה ל-409
            var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(
                () => repoB.SaveChangesAsync());

            Assert.IsType<DbUpdateConcurrencyException>(ex.InnerException);
        }

        private WatchStoreDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<WatchStoreDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

            return new WatchStoreDbContext(options);
        }
    }
}
