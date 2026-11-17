using WatchStore.Core.DTOs;

namespace WatchStore.Core.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<PagedResult<AppointmentResponse>> GetPagedAsync(
            AppointmentQueryParameters query,
            CancellationToken cancellationToken = default);

        Task<bool> CancelAsync(
            int id,
            CancelAppointmentRequest request,
            CancellationToken cancellationToken = default);
    }
}
