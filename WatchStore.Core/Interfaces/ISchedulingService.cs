using WatchStore.Core.DTOs;

namespace WatchStore.Core.Interfaces
{
    public interface ISchedulingService
    {
        Task<List<AppointmentTypeResponse>> GetAppointmentTypesAsync(
            CancellationToken cancellationToken = default);

        Task<AvailabilityResponse> GetAvailabilityAsync(
            AvailabilityQuery query,
            CancellationToken cancellationToken = default);

        Task<AppointmentResponse> BookAsync(
            BookAppointmentRequest request,
            CancellationToken cancellationToken = default);
    }
}
