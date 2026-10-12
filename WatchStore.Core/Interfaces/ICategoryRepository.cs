using WatchStore.Core.Entities;

namespace WatchStore.Core.Interfaces
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<List<Category>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<bool> HasProductsAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Category category,
            CancellationToken cancellationToken = default);

        void Update(Category category);

        void Delete(Category category);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}