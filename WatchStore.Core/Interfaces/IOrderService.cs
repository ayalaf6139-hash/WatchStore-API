using WatchStore.Core.DTOs;

namespace WatchStore.Core.Interfaces
{
    public interface IOrderService
    {
        // requesterStoreId: null עבור Admin (רואה הכול), אחרת החנות של המשתמש המחובר
        Task<OrderResponse?> GetByIdAsync(
            int id,
            int? requesterStoreId,
            CancellationToken cancellationToken = default);

        Task<List<OrderResponse>> GetAllAsync(
            int? requesterStoreId,
            CancellationToken cancellationToken = default);

        // storeId נלקח מה-token ולא מגוף הבקשה
        Task<OrderResponse> CreateAsync(
            int storeId,
            CreateOrderRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateStatusAsync(
            int id,
            UpdateOrderStatusRequest request,
            CancellationToken cancellationToken = default);
    }
}
