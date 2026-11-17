using WatchStore.Core.Entities;

namespace WatchStore.Core.Interfaces
{
    public interface IBusinessSettingsRepository
    {
        Task<Business?> GetBusinessAsync(
            int businessId,
            CancellationToken cancellationToken = default);

        Task<List<OpeningHours>> GetOpeningHoursAsync(
            int businessId,
            CancellationToken cancellationToken = default);

        Task<List<BlockedPeriod>> GetBlockedPeriodsAsync(
            int businessId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default);

        Task<List<AppointmentResource>> GetActiveResourcesAsync(
            int businessId,
            CancellationToken cancellationToken = default);

        Task<List<AppointmentType>> GetAppointmentTypesAsync(
            int businessId,
            bool includeInactive,
            CancellationToken cancellationToken = default);

        Task<AppointmentType?> GetAppointmentTypeWithRulesAsync(
            int appointmentTypeId,
            CancellationToken cancellationToken = default);
    }
}
