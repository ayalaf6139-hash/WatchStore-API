using WatchStore.Core.DTOs;

namespace WatchStore.Core.Interfaces
{
    // חנויות נוצרות דרך ההרשמה (IAuthService); ה-Admin רק צופה בהן
    public interface IStoreService
    {
        Task<StoreResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<List<StoreResponse>> GetAllAsync(
            CancellationToken cancellationToken = default);
    }
}
