using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;

namespace WatchStore.Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly WatchStoreDbContext _context;

        public ProductRepository(WatchStoreDbContext context)
        {
            _context = context;
        }

        public async Task<Product?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .FirstOrDefaultAsync(
                    p => p.Id == id,
                    cancellationToken);
        }

        public async Task<List<Product>> GetByIdsAsync(
            IEnumerable<int> ids,
            CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();

            // WHERE Id IN (...) - שאילתה אחת לכל המוצרים בהזמנה
            return await _context.Products
                .Where(p => idList.Contains(p.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ModelNumberExistsAsync(
            string modelNumber,
            int? excludeProductId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AnyAsync(
                    p => p.ModelNumber == modelNumber &&
                         p.Id != excludeProductId,
                    cancellationToken);
        }

        public async Task<Product?> GetByIdWithDetailsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductTags)
                    .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(
                    p => p.Id == id,
                    cancellationToken);
        }

        public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
            ProductQueryParameters query,
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            // IQueryable - שום דבר לא נשלח לבסיס הנתונים עד ToListAsync/CountAsync
            IQueryable<Product> products = _context.Products
                .AsNoTracking();

            if (!includeInactive)
                products = products.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                products = products.Where(p =>
                    p.Name.Contains(search) ||
                    p.ModelNumber.Contains(search) ||
                    p.Brand.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(query.Brand))
                products = products.Where(p => p.Brand == query.Brand);

            if (query.CategoryId.HasValue)
                products = products.Where(p => p.CategoryId == query.CategoryId);

            if (query.MinPrice.HasValue)
                products = products.Where(p => p.WholesalePrice >= query.MinPrice);

            if (query.MaxPrice.HasValue)
                products = products.Where(p => p.WholesalePrice <= query.MaxPrice);

            var totalCount = await products.CountAsync(cancellationToken);

            var descending = string.Equals(
                query.SortDirection,
                "desc",
                StringComparison.OrdinalIgnoreCase);

            products = query.SortBy?.ToLowerInvariant() switch
            {
                "name" => descending
                    ? products.OrderByDescending(p => p.Name)
                    : products.OrderBy(p => p.Name),
                "price" => descending
                    ? products.OrderByDescending(p => p.WholesalePrice)
                    : products.OrderBy(p => p.WholesalePrice),
                "stock" => descending
                    ? products.OrderByDescending(p => p.StockQuantity)
                    : products.OrderBy(p => p.StockQuantity),
                "createdat" => descending
                    ? products.OrderByDescending(p => p.CreatedAt)
                    : products.OrderBy(p => p.CreatedAt),
                _ => products.OrderBy(p => p.Id)
            };

            // Skip/Take בתוך השאילתה - ה-pagination מתבצע ב-SQL ולא בזיכרון
            var items = await products
                .Include(p => p.Category)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task AddAsync(
            Product product,
            CancellationToken cancellationToken = default)
        {
            await _context.Products.AddAsync(
                product,
                cancellationToken);
        }

        public void Update(Product product)
        {
            _context.Products.Update(product);
        }

        public void Delete(Product product)
        {
            _context.Products.Remove(product);
        }

        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesWithConcurrencyCheckAsync(cancellationToken);
        }
    }
}
