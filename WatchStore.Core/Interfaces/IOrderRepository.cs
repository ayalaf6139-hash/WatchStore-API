using WatchStore.Core.Entities;

namespace WatchStore.Core.Interfaces
{
    public interface IOrderRepository
    {
        // מחזיר ישות במעקב (tracked) - לשימוש כשמתכוונים לשנות ולשמור
        Task<Order?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        // קריאה בלבד: ההזמנה + הפריטים + המוצרים בשאילתה אחת (ללא N+1)
        Task<Order?> GetByIdWithItemsAsync(
            int id,
            CancellationToken cancellationToken = default);

        // storeId: null מחזיר את כל ההזמנות, אחרת רק של החנות הזו
        Task<List<Order>> GetAllWithItemsAsync(
            int? storeId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Order order,
            CancellationToken cancellationToken = default);

        void Update(Order order);

        void Delete(Order order);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}