using Microsoft.Extensions.Logging;
using WatchStore.Core.Common;
using WatchStore.Core.DTOs;
using WatchStore.Core.Enums;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;
using WatchStore.Service.Scheduling;

namespace WatchStore.Service.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IBusinessSettingsRepository _settingsRepository;
        private readonly WatchFlowSettings _settings;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IBusinessSettingsRepository settingsRepository,
            WatchFlowSettings settings,
            TimeProvider timeProvider,
            ILogger<AppointmentService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _settingsRepository = settingsRepository;
            _settings = settings;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<AppointmentResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var appointment = await _appointmentRepository.GetByIdWithDetailsAsync(id, cancellationToken);

            if (appointment == null || appointment.BusinessId != _settings.BusinessId)
                return null;

            var timeZone = await GetTimeZoneAsync(cancellationToken);

            return AppointmentMapper.ToResponse(appointment, timeZone);
        }

        public async Task<PagedResult<AppointmentResponse>> GetPagedAsync(
            AppointmentQueryParameters query,
            CancellationToken cancellationToken = default)
        {
            if (query.FromDate.HasValue && query.ToDate.HasValue && query.ToDate < query.FromDate)
                throw new BusinessRuleException("תאריך הסיום חייב להיות באותו יום או אחרי תאריך ההתחלה.");

            var timeZone = await GetTimeZoneAsync(cancellationToken);

            DateTime? fromUtc = query.FromDate.HasValue
                ? BusinessTime.StartOfLocalDayUtc(query.FromDate.Value, timeZone)
                : null;

            DateTime? toUtc = query.ToDate.HasValue
                ? BusinessTime.StartOfLocalDayUtc(query.ToDate.Value.AddDays(1), timeZone)
                : null;

            var (items, totalCount) = await _appointmentRepository.GetPagedAsync(
                _settings.BusinessId,
                fromUtc,
                toUtc,
                query.Status,
                query.Page,
                query.PageSize,
                cancellationToken);

            return new PagedResult<AppointmentResponse>
            {
                Items = items.Select(a => AppointmentMapper.ToResponse(a, timeZone)).ToList(),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<bool> CancelAsync(
            int id,
            CancelAppointmentRequest request,
            CancellationToken cancellationToken = default)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken);

            if (appointment == null || appointment.BusinessId != _settings.BusinessId)
                return false;

            if (appointment.Status != AppointmentStatus.Booked)
                throw new BusinessRuleException($"לא ניתן לבטל תור בסטטוס {appointment.Status}.");

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancelledAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            appointment.CancellationReason = request.Reason;

            try
            {
                await _appointmentRepository.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                throw new ConflictException("התור עודכן על ידי משתמש אחר. אנא רענן ונסה שוב.");
            }

            _logger.LogInformation("Appointment {AppointmentId} cancelled", id);

            return true;
        }

        private async Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken cancellationToken)
        {
            var business = await _settingsRepository.GetBusinessAsync(_settings.BusinessId, cancellationToken)
                           ?? throw new InvalidOperationException($"Business {_settings.BusinessId} is not configured.");

            return BusinessTime.ResolveTimeZone(business.TimeZoneId);
        }
    }
}
