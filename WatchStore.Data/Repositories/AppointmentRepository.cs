using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Core.Enums;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;

namespace WatchStore.Data.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly WatchStoreDbContext _context;

        public AppointmentRepository(WatchStoreDbContext context)
        {
            _context = context;
        }

        public async Task<Appointment?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<Appointment?> GetByIdWithDetailsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Customer)
                .Include(a => a.AppointmentType)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<(List<Appointment> Items, int TotalCount)> GetPagedAsync(
            int businessId,
            DateTime? fromUtc,
            DateTime? toUtc,
            AppointmentStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Appointment> appointments = _context.Appointments
                .AsNoTracking()
                .Where(a => a.BusinessId == businessId);

            if (fromUtc.HasValue)
                appointments = appointments.Where(a => a.StartUtc >= fromUtc.Value);

            if (toUtc.HasValue)
                appointments = appointments.Where(a => a.StartUtc < toUtc.Value);

            if (status.HasValue)
                appointments = appointments.Where(a => a.Status == status.Value);

            var totalCount = await appointments.CountAsync(cancellationToken);

            var items = await appointments
                .Include(a => a.Customer)
                .Include(a => a.AppointmentType)
                .OrderBy(a => a.StartUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<Appointment?> GetByIdempotencyKeyAsync(
            int businessId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            return await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.BusinessId == businessId &&
                         a.IdempotencyKey == idempotencyKey,
                    cancellationToken);
        }

        public async Task<List<Appointment>> GetBookedInRangeAsync(
            int businessId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default)
        {
            return await _context.Appointments
                .AsNoTracking()
                .Where(a => a.BusinessId == businessId &&
                            a.Status == AppointmentStatus.Booked &&
                            a.ReservedStartUtc < toUtc &&
                            a.ReservedEndUtc > fromUtc)
                .OrderBy(a => a.ReservedStartUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Appointment>> GetByCustomerAsync(
            int customerId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Appointments
                .AsNoTracking()
                .Include(a => a.AppointmentType)
                .Where(a => a.CustomerId == customerId)
                .OrderByDescending(a => a.StartUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            Appointment appointment,
            CancellationToken cancellationToken = default)
        {
            await _context.Appointments.AddAsync(appointment, cancellationToken);
        }

        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesWithConcurrencyCheckAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.IsExclusionViolation(DbConstraintNames.AppointmentNoOverlap))
            {
                throw new AppointmentSlotConflictException(
                    "The requested time overlaps an existing booked appointment.",
                    ex);
            }
            catch (DbUpdateException ex) when (ex.IsUniqueViolation(DbConstraintNames.AppointmentIdempotencyKey))
            {
                throw new DuplicateKeyException(DbConstraintNames.AppointmentIdempotencyKey, ex);
            }
        }
    }
}
