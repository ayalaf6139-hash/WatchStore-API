using WatchStore.Core.Entities;
using WatchStore.Core.Enums;

namespace WatchStore.Core.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<Appointment?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<Appointment?> GetByIdWithDetailsAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<(List<Appointment> Items, int TotalCount)> GetPagedAsync(
            int businessId,
            DateTime? fromUtc,
            DateTime? toUtc,
            AppointmentStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<Appointment?> GetByIdempotencyKeyAsync(
            int businessId,
            string idempotencyKey,
            CancellationToken cancellationToken = default);

        Task<List<Appointment>> GetBookedInRangeAsync(
            int businessId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default);

        Task<List<Appointment>> GetByCustomerAsync(
            int customerId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Appointment appointment,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
