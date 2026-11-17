using WatchStore.Core.Entities;

namespace WatchStore.Core.Interfaces
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<Customer?> GetByPhoneAsync(
            int businessId,
            string normalizedPhoneNumber,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Customer customer,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
