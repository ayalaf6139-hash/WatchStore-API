using WatchStore.Core.DTOs;

namespace WatchStore.Core.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<CustomerResponse?> FindByPhoneAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default);

        Task<CustomerResponse> CreateAsync(
            CreateCustomerRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(
            int id,
            UpdateCustomerRequest request,
            CancellationToken cancellationToken = default);

        Task<List<AppointmentResponse>> GetAppointmentsAsync(
            int customerId,
            CancellationToken cancellationToken = default);
    }
}
