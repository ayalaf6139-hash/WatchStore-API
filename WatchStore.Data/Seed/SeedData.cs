using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Core.Enums;

namespace WatchStore.Data.Seed
{
    // נתוני דוגמה שנכנסים לבסיס הנתונים דרך ה-migrations,
    // כדי שאפשר יהיה להריץ את המערכת מיד אחרי הקמתה
    public static class SeedData
    {
        // תאריך קבוע - DateTime.UtcNow היה יוצר migration חדש בכל פעם
        private static readonly DateTime SeedDate =
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static void Seed(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Men", Description = "שעוני גברים" },
                new Category { Id = 2, Name = "Women", Description = "שעוני נשים" },
                new Category { Id = 3, Name = "Sport", Description = "שעונים ספורטיביים" },
                new Category { Id = 4, Name = "Digital", Description = "שעונים דיגיטליים" },
                new Category { Id = 5, Name = "Luxury", Description = "שעונים יוקרתיים" });

            modelBuilder.Entity<Tag>().HasData(
                new Tag { Id = 1, Name = "Waterproof" },
                new Tag { Id = 2, Name = "Sport" },
                new Tag { Id = 3, Name = "Digital" },
                new Tag { Id = 4, Name = "Luxury" },
                new Tag { Id = 5, Name = "Classic" });

            modelBuilder.Entity<Product>().HasData(
                // יחידה אחת בלבד במלאי - לצורך הדגמת התחרות על המשאב המוגבל
                CreateProduct(1, "ROLEX-001", "Submariner Date", "Rolex", 5, 32000m, 45000m, 1),
                CreateProduct(2, "CASIO-001", "G-Shock GA-2100", "Casio", 4, 350m, 550m, 40),
                CreateProduct(3, "CASIO-002", "Vintage A168", "Casio", 4, 120m, 220m, 60),
                CreateProduct(4, "SEIKO-001", "Presage Cocktail Time", "Seiko", 1, 1400m, 2100m, 12),
                CreateProduct(5, "TISSOT-001", "PRX Powermatic 80", "Tissot", 1, 2300m, 3200m, 8),
                CreateProduct(6, "FOSSIL-001", "Jacqueline", "Fossil", 2, 380m, 620m, 25),
                CreateProduct(7, "GARMIN-001", "Forerunner 265", "Garmin", 3, 1500m, 2100m, 15),
                CreateProduct(8, "OMEGA-001", "Seamaster Diver 300M", "Omega", 5, 19000m, 26000m, 2));

            modelBuilder.Entity<ProductTag>().HasData(
                new ProductTag { ProductId = 1, TagId = 1 },
                new ProductTag { ProductId = 1, TagId = 4 },
                new ProductTag { ProductId = 2, TagId = 1 },
                new ProductTag { ProductId = 2, TagId = 2 },
                new ProductTag { ProductId = 2, TagId = 3 },
                new ProductTag { ProductId = 3, TagId = 3 },
                new ProductTag { ProductId = 3, TagId = 5 },
                new ProductTag { ProductId = 4, TagId = 5 },
                new ProductTag { ProductId = 5, TagId = 5 },
                new ProductTag { ProductId = 6, TagId = 5 },
                new ProductTag { ProductId = 7, TagId = 1 },
                new ProductTag { ProductId = 7, TagId = 2 },
                new ProductTag { ProductId = 7, TagId = 3 },
                new ProductTag { ProductId = 8, TagId = 1 },
                new ProductTag { ProductId = 8, TagId = 4 });

            modelBuilder.Entity<Store>().HasData(
                new Store
                {
                    Id = 1,
                    Name = "Time Center Tel Aviv",
                    ContactName = "Dana Levi",
                    Phone = "03-5551234",
                    Email = "dana@timecenter.example",
                    Address = "Dizengoff 50, Tel Aviv",
                    IsActive = true
                },
                new Store
                {
                    Id = 2,
                    Name = "Jerusalem Watches",
                    ContactName = "Yossi Cohen",
                    Phone = "02-5554321",
                    Email = "yossi@jlmwatches.example",
                    Address = "Jaffa 20, Jerusalem",
                    IsActive = true
                },
                new Store
                {
                    Id = 3,
                    Name = "Haifa Watch House",
                    ContactName = "Noa Mizrahi",
                    Phone = "04-5559876",
                    Email = "noa@haifawatch.example",
                    Address = "Herzl 15, Haifa",
                    IsActive = true
                });

            // משתמשי דמו. ה-hash חושב מראש עם BCrypt (work factor 11),
            // כי hash חדש בכל הרצה היה יוצר migration חדש בכל פעם.
            // Admin123! / Store123! - הסיסמאות עצמן מופיעות רק ב-README
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "$2a$11$lfcQS78hRc1tJvdwcOEGpO2LKjJQOs0ubAPCe8a.FBLa0/L7LHtWi",
                    Role = UserRole.Admin,
                    StoreId = null
                },
                new User
                {
                    Id = 2,
                    Username = "store1",
                    PasswordHash = "$2a$11$z3wgO5Eb7LnRVWdxMOmFvONVpZCsfgw/OUiq2l0rcUEotW.q7wksC",
                    Role = UserRole.Store,
                    StoreId = 1
                },
                new User
                {
                    Id = 3,
                    Username = "store2",
                    PasswordHash = "$2a$11$z3wgO5Eb7LnRVWdxMOmFvONVpZCsfgw/OUiq2l0rcUEotW.q7wksC",
                    Role = UserRole.Store,
                    StoreId = 2
                });
        }

        private static Product CreateProduct(
            int id,
            string modelNumber,
            string name,
            string brand,
            int categoryId,
            decimal wholesalePrice,
            decimal recommendedRetailPrice,
            int stockQuantity)
        {
            return new Product
            {
                Id = id,
                ModelNumber = modelNumber,
                Name = name,
                Brand = brand,
                CategoryId = categoryId,
                WholesalePrice = wholesalePrice,
                RecommendedRetailPrice = recommendedRetailPrice,
                StockQuantity = stockQuantity,
                IsActive = true,
                CreatedAt = SeedDate
            };
        }
    }
}
