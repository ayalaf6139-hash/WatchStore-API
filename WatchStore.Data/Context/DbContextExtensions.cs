using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Exceptions;

namespace WatchStore.Data.Context
{
    public static class DbContextExtensions
    {
        // שמירה שמתרגמת התנגשות של RowVersion לחריגה של ה-Core.
        // כך ה-Service יודע לטפל בהתנגשות בלי להכיר את EF Core
        public static async Task SaveChangesWithConcurrencyCheckAsync(
            this DbContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrencyConflictException(
                    "The row was modified by another user since it was read.",
                    ex);
            }
        }
    }
}
