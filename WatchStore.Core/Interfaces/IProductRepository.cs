using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;

namespace WatchStore.Core.Interfaces
{
    public interface IProductRepository
    {
        // מחזיר ישות במעקב (tracked) - לשימוש כשמתכוונים לשנות ולשמור
        Task<Product?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        // מחזיר כמה מוצרים במעקב בשאילתה אחת - במקום שאילתה לכל פריט בהזמנה (N+1)
        Task<List<Product>> GetByIdsAsync(
            IEnumerable<int> ids,
            CancellationToken cancellationToken = default);

        Task<bool> ModelNumberExistsAsync(
            string modelNumber,
            int? excludeProductId,
            CancellationToken cancellationToken = default);

        // קריאה בלבד: כולל קטגוריה ותגיות, ללא מעקב
        Task<Product?> GetByIdWithDetailsAsync(
            int id,
            CancellationToken cancellationToken = default);

        // סינון, מיון ו-pagination מתבצעים בבסיס הנתונים
        Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
            ProductQueryParameters query,
            bool includeInactive,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Product product,
            CancellationToken cancellationToken = default);

        void Update(Product product);

        void Delete(Product product);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}

