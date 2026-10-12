using WatchStore.Core.Entities;

namespace WatchStore.Core.Interfaces
{
    public interface ITagRepository
    {
        Task<Tag?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<List<Tag>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Tag tag,
            CancellationToken cancellationToken = default);

        void Update(Tag tag);

        void Delete(Tag tag);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
