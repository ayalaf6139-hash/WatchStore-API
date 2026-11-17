using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;

namespace WatchStore.Data.Repositories
{
    public class BusinessSettingsRepository : IBusinessSettingsRepository
    {
        private readonly WatchStoreDbContext _context;

        public BusinessSettingsRepository(WatchStoreDbContext context)
        {
            _context = context;
        }

        public async Task<Business?> GetBusinessAsync(
            int businessId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        }

        public async Task<List<OpeningHours>> GetOpeningHoursAsync(
            int businessId,
            CancellationToken cancellationToken = default)
        {
            return await _context.OpeningHours
                .AsNoTracking()
                .Where(h => h.BusinessId == businessId)
                .OrderBy(h => h.DayOfWeek)
                .ThenBy(h => h.OpenTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<BlockedPeriod>> GetBlockedPeriodsAsync(
            int businessId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default)
        {
            return await _context.BlockedPeriods
                .AsNoTracking()
                .Where(b => b.BusinessId == businessId &&
                            b.StartUtc < toUtc &&
                            b.EndUtc > fromUtc)
                .OrderBy(b => b.StartUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AppointmentResource>> GetActiveResourcesAsync(
            int businessId,
            CancellationToken cancellationToken = default)
        {
            return await _context.AppointmentResources
                .AsNoTracking()
                .Where(r => r.BusinessId == businessId && r.IsActive)
                .OrderBy(r => r.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AppointmentType>> GetAppointmentTypesAsync(
            int businessId,
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            return await _context.AppointmentTypes
                .AsNoTracking()
                .Where(t => t.BusinessId == businessId && (includeInactive || t.IsActive))
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<AppointmentType?> GetAppointmentTypeWithRulesAsync(
            int appointmentTypeId,
            CancellationToken cancellationToken = default)
        {
            return await _context.AppointmentTypes
                .AsNoTracking()
                .Include(t => t.DurationRules)
                .FirstOrDefaultAsync(t => t.Id == appointmentTypeId, cancellationToken);
        }
    }
}
