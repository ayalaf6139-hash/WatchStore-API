using WatchStore.Core.DTOs;

namespace WatchStore.Core.Interfaces
{
    public interface ITagService
    {
        Task<TagResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<List<TagResponse>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task<TagResponse> CreateAsync(
            CreateTagRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(
            int id,
            UpdateTagRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default);
    }
}
