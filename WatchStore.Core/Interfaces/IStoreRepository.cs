using WatchStore.Core.Entities;

namespace WatchStore.Core.Interfaces
{
    public interface IStoreRepository
    {
        Task<Store?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<List<Store>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Store store,
            CancellationToken cancellationToken = default);

        void Update(Store store);

        void Delete(Store store);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}