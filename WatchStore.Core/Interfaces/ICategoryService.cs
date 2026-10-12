using WatchStore.Core.DTOs;

namespace WatchStore.Core.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<List<CategoryResponse>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task<CategoryResponse> CreateAsync(
            CreateCategoryRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(
            int id,
            UpdateCategoryRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default);
    }
}