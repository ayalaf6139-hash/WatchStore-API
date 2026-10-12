using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;

namespace WatchStore.Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly WatchStoreDbContext _context;

        public OrderRepository(WatchStoreDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .FirstOrDefaultAsync(
                    o => o.Id == id,
                    cancellationToken);
        }

        public async Task<Order?> GetByIdWithItemsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(
                    o => o.Id == id,
                    cancellationToken);
        }

        public async Task<List<Order>> GetAllWithItemsAsync(
            int? storeId,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Order> orders = _context.Orders
                .AsNoTracking();

            if (storeId.HasValue)
                orders = orders.Where(o => o.StoreId == storeId);

            return await orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            Order order,
            CancellationToken cancellationToken = default)
        {
            await _context.Orders.AddAsync(
                order,
                cancellationToken);
        }

        public void Update(Order order)
        {
            _context.Orders.Update(order);
        }

        public void Delete(Order order)
        {
            _context.Orders.Remove(order);
        }

        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            // הזמנה חדשה מעדכנת גם את המלאי של המוצרים, ולכן גם כאן
            // התנגשות על ה-RowVersion של מוצר מתורגמת לחריגה של ה-Core
            await _context.SaveChangesWithConcurrencyCheckAsync(cancellationToken);
        }
    }
}