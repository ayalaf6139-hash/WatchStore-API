using WatchStore.Core.DTOs;

namespace WatchStore.Core.Interfaces
{
    public interface IProductService
    {
        Task<ProductResponse> CreateAsync(
            CreateProductRequest request,
            CancellationToken cancellationToken = default);

        // includeInactive: רק Admin רואה מוצרים שאינם פעילים
        Task<PagedResult<ProductListResponse>> GetPagedAsync(
            ProductQueryParameters query,
            bool includeInactive,
            CancellationToken cancellationToken = default);

        Task<ProductResponse?> GetByIdAsync(
            int id,
            bool includeInactive,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(
            int id,
            UpdateProductRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default);
    }
}