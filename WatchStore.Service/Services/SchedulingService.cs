using Microsoft.Extensions.Logging;
using WatchStore.Core.Common;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;
using WatchStore.Core.Enums;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;
using WatchStore.Service.Scheduling;

namespace WatchStore.Service.Services
{
    public class SchedulingService : ISchedulingService
    {
        public const string SlotTakenMessage = "השעה שנבחרה אינה פנויה. אלה המועדים הפנויים הקרובים.";

        private readonly IBusinessSettingsRepository _settingsRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly WatchFlowSettings _settings;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<SchedulingService> _logger;

        public SchedulingService(
            IBusinessSettingsRepository settingsRepository,
            IAppointmentRepository appointmentRepository,
            ICustomerRepository customerRepository,
            WatchFlowSettings settings,
            TimeProvider timeProvider,
            ILogger<SchedulingService> logger)
        {
            _settingsRepository = settingsRepository;
            _appointmentRepository = appointmentRepository;
            _customerRepository = customerRepository;
            _settings = settings;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<List<AppointmentTypeResponse>> GetAppointmentTypesAsync(
            CancellationToken cancellationToken = default)
        {
            var types = await _settingsRepository.GetAppointmentTypesAsync(
                _settings.BusinessId,
                includeInactive: false,
                cancellationToken);

            return types
                .Select(t => new AppointmentTypeResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    BaseDurationMinutes = t.BaseDurationMinutes,
                    BufferBeforeMinutes = t.BufferBeforeMinutes,
                    BufferAfterMinutes = t.BufferAfterMinutes
                })
                .ToList();
        }

        public async Task<AvailabilityResponse> GetAvailabilityAsync(
            AvailabilityQuery query,
            CancellationToken cancellationToken = default)
        {
            if (query.FromDate == default)
                throw new BusinessRuleException("יש לציין תאריך התחלה לחיפוש (FromDate).");

            var toDate = query.ToDate ?? query.FromDate.AddDays(_settings.DefaultSearchDays - 1);

            if (toDate < query.FromDate)
                throw new BusinessRuleException("תאריך הסיום חייב להיות באותו יום או אחרי תאריך ההתחלה.");

            if (toDate.DayNumber - query.FromDate.DayNumber + 1 > _settings.MaxSearchDays)
                throw new BusinessRuleException($"אפשר לחפש עד {_settings.MaxSearchDays} ימים בבקשה אחת.");

            var context = await LoadContextAsync(query.AppointmentTypeId, cancellationToken);

            var duration = AppointmentDurationCalculator.Calculate(
                context.AppointmentType,
                query.CustomerType,
                query.RequiresSpecialPreparation);

            var slots = await FindSlotsAsync(
                context,
                duration,
                query.FromDate,
                toDate,
                query.MaxResults,
                cancellationToken);

            return new AvailabilityResponse
            {
                AppointmentTypeId = context.AppointmentType.Id,
                DurationMinutes = duration.DurationMinutes,
                TimeZoneId = context.TimeZone.Id,
                Slots = slots.Select(s => AppointmentMapper.ToSlotResponse(s, context.TimeZone)).ToList()
            };
        }

        public async Task<AppointmentResponse> BookAsync(
            BookAppointmentRequest request,
            CancellationToken cancellationToken = default)
        {
            var startUtc = BusinessTime.EnsureUtc(request.StartUtc);
            var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? null
                : request.IdempotencyKey.Trim();

            if (idempotencyKey != null)
            {
                var previous = await _appointmentRepository.GetByIdempotencyKeyAsync(
                    _settings.BusinessId,
                    idempotencyKey,
                    cancellationToken);

                if (previous != null)
                    return await ReplayAsync(previous, request, startUtc, cancellationToken);
            }

            var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);

            if (customer == null || customer.BusinessId != _settings.BusinessId)
                throw new NotFoundException($"לקוח {request.CustomerId} לא נמצא.");

            var context = await LoadContextAsync(request.AppointmentTypeId, cancellationToken);

            var duration = AppointmentDurationCalculator.Calculate(
                context.AppointmentType,
                customer.CustomerType,
                request.RequiresSpecialPreparation);

            var requestedDate = BusinessTime.ToLocalDate(startUtc, context.TimeZone);

            var slot = (await FindSlotsAsync(context, duration, requestedDate, requestedDate, int.MaxValue, cancellationToken))
                .FirstOrDefault(s => s.StartUtc == startUtc);

            if (slot == null)
            {
                _logger.LogWarning(
                    "Booking rejected: {StartUtc} is not an available slot for appointment type {AppointmentTypeId}",
                    startUtc,
                    context.AppointmentType.Id);

                throw await CreateSlotUnavailableAsync(context, duration, startUtc, cancellationToken);
            }

            var appointment = new Appointment
            {
                BusinessId = _settings.BusinessId,
                CustomerId = customer.Id,
                AppointmentTypeId = context.AppointmentType.Id,
                AppointmentResourceId = slot.ResourceId,
                ConversationId = request.ConversationId,
                StartUtc = slot.StartUtc,
                EndUtc = slot.EndUtc,
                ReservedStartUtc = slot.ReservedStartUtc,
                ReservedEndUtc = slot.ReservedEndUtc,
                DurationMinutes = duration.DurationMinutes,
                Status = AppointmentStatus.Booked,
                IdempotencyKey = idempotencyKey,
                CustomerNotes = request.CustomerNotes,
                PreparationNotes = request.PreparationNotes
            };

            await _appointmentRepository.AddAsync(appointment, cancellationToken);

            try
            {
                await _appointmentRepository.SaveChangesAsync(cancellationToken);
            }
            catch (AppointmentSlotConflictException)
            {
                _logger.LogWarning(
                    "Booking conflict: slot {StartUtc} on resource {ResourceId} was taken concurrently",
                    slot.StartUtc,
                    slot.ResourceId);

                throw await CreateSlotUnavailableAsync(context, duration, startUtc, cancellationToken);
            }
            catch (DuplicateKeyException) when (idempotencyKey != null)
            {
                var previous = await _appointmentRepository.GetByIdempotencyKeyAsync(
                    _settings.BusinessId,
                    idempotencyKey,
                    cancellationToken);

                if (previous == null)
                    throw;

                return await ReplayAsync(previous, request, startUtc, cancellationToken);
            }

            _logger.LogInformation(
                "Appointment {AppointmentId} booked for customer {CustomerId} at {StartUtc} ({Duration} min)",
                appointment.Id,
                customer.Id,
                appointment.StartUtc,
                appointment.DurationMinutes);

            return AppointmentMapper.ToResponse(appointment, customer, context.AppointmentType, context.TimeZone);
        }

        private async Task<AppointmentResponse> ReplayAsync(
            Appointment previous,
            BookAppointmentRequest request,
            DateTime startUtc,
            CancellationToken cancellationToken)
        {
            if (previous.CustomerId != request.CustomerId ||
                previous.AppointmentTypeId != request.AppointmentTypeId ||
                previous.StartUtc != startUtc)
            {
                throw new ConflictException("מזהה הבקשה (IdempotencyKey) כבר שימש לקביעת תור אחר.");
            }

            var details = await _appointmentRepository.GetByIdWithDetailsAsync(previous.Id, cancellationToken)
                          ?? throw new NotFoundException($"תור {previous.Id} לא נמצא.");

            var timeZone = await GetTimeZoneAsync(cancellationToken);

            _logger.LogInformation(
                "Idempotent replay: returning existing appointment {AppointmentId}",
                details.Id);

            return AppointmentMapper.ToResponse(details, timeZone);
        }

        private async Task<SlotUnavailableException> CreateSlotUnavailableAsync(
            ScheduleContext context,
            AppointmentDuration duration,
            DateTime requestedStartUtc,
            CancellationToken cancellationToken)
        {
            var earliest = EarliestStartUtc();
            var searchFrom = requestedStartUtc > earliest ? requestedStartUtc : earliest;
            var fromDate = BusinessTime.ToLocalDate(searchFrom, context.TimeZone);
            var toDate = fromDate.AddDays(_settings.MaxSearchDays - 1);

            var candidates = await FindSlotsAsync(context, duration, fromDate, toDate, int.MaxValue, cancellationToken);

            var alternatives = candidates
                .OrderBy(s => Math.Abs((s.StartUtc - requestedStartUtc).Ticks))
                .Take(_settings.AlternativesCount)
                .OrderBy(s => s.StartUtc)
                .Select(s => AppointmentMapper.ToSlotResponse(s, context.TimeZone))
                .ToList();

            return new SlotUnavailableException(SlotTakenMessage, alternatives);
        }

        private async Task<List<CandidateSlot>> FindSlotsAsync(
            ScheduleContext context,
            AppointmentDuration duration,
            DateOnly fromDate,
            DateOnly toDate,
            int maxResults,
            CancellationToken cancellationToken)
        {
            var rangeStartUtc = DateTime.SpecifyKind(fromDate.AddDays(-1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var rangeEndUtc = DateTime.SpecifyKind(toDate.AddDays(2).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

            var booked = await _appointmentRepository.GetBookedInRangeAsync(
                _settings.BusinessId,
                rangeStartUtc,
                rangeEndUtc,
                cancellationToken);

            var blocked = await _settingsRepository.GetBlockedPeriodsAsync(
                _settings.BusinessId,
                rangeStartUtc,
                rangeEndUtc,
                cancellationToken);

            var busyPeriods = booked
                .Select(a => new BusyPeriod(a.AppointmentResourceId, a.ReservedStartUtc, a.ReservedEndUtc))
                .Concat(blocked.Select(b => new BusyPeriod(b.AppointmentResourceId, b.StartUtc, b.EndUtc)))
                .ToList();

            return SlotFinder.FindSlots(new SlotSearch
            {
                TimeZone = context.TimeZone,
                FromDate = fromDate,
                ToDate = toDate,
                OpeningHours = context.OpeningHours,
                ResourceIds = context.ResourceIds,
                BusyPeriods = busyPeriods,
                Duration = duration,
                SlotIntervalMinutes = _settings.SlotIntervalMinutes,
                EarliestStartUtc = EarliestStartUtc(),
                MaxResults = maxResults
            });
        }

        private DateTime EarliestStartUtc()
        {
            return _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(_settings.MinimumNoticeMinutes);
        }

        private async Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken cancellationToken)
        {
            var business = await _settingsRepository.GetBusinessAsync(_settings.BusinessId, cancellationToken)
                           ?? throw new InvalidOperationException($"Business {_settings.BusinessId} is not configured.");

            return BusinessTime.ResolveTimeZone(business.TimeZoneId);
        }

        private async Task<ScheduleContext> LoadContextAsync(
            int appointmentTypeId,
            CancellationToken cancellationToken)
        {
            var timeZone = await GetTimeZoneAsync(cancellationToken);

            var appointmentType = await _settingsRepository.GetAppointmentTypeWithRulesAsync(
                appointmentTypeId,
                cancellationToken);

            if (appointmentType == null ||
                !appointmentType.IsActive ||
                appointmentType.BusinessId != _settings.BusinessId)
            {
                throw new NotFoundException($"סוג תור {appointmentTypeId} לא נמצא.");
            }

            var openingHours = await _settingsRepository.GetOpeningHoursAsync(
                _settings.BusinessId,
                cancellationToken);

            var resources = await _settingsRepository.GetActiveResourcesAsync(
                _settings.BusinessId,
                cancellationToken);

            if (resources.Count == 0)
                throw new BusinessRuleException("לא הוגדרו משאבים פעילים לקביעת תורים.");

            return new ScheduleContext(
                appointmentType,
                openingHours,
                resources.Select(r => r.Id).ToList(),
                timeZone);
        }

        private sealed record ScheduleContext(
            AppointmentType AppointmentType,
            IReadOnlyList<OpeningHours> OpeningHours,
            IReadOnlyList<int> ResourceIds,
            TimeZoneInfo TimeZone);
    }
}
