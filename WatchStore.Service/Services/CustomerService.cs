using AutoMapper;
using Microsoft.Extensions.Logging;
using WatchStore.Core.Common;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;
using WatchStore.Service.Scheduling;

namespace WatchStore.Service.Services
{
    public class CustomerService : ICustomerService
    {
        public const string InvalidPhoneMessage = "מספר הטלפון אינו תקין.";
        public const string DuplicatePhoneMessage = "לקוח עם מספר הטלפון הזה כבר קיים.";

        private readonly ICustomerRepository _customerRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IBusinessSettingsRepository _settingsRepository;
        private readonly WatchFlowSettings _settings;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            ICustomerRepository customerRepository,
            IAppointmentRepository appointmentRepository,
            IBusinessSettingsRepository settingsRepository,
            WatchFlowSettings settings,
            IMapper mapper,
            ILogger<CustomerService> logger)
        {
            _customerRepository = customerRepository;
            _appointmentRepository = appointmentRepository;
            _settingsRepository = settingsRepository;
            _settings = settings;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CustomerResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var customer = await GetOwnCustomerAsync(id, cancellationToken);

            return customer == null ? null : _mapper.Map<CustomerResponse>(customer);
        }

        public async Task<CustomerResponse?> FindByPhoneAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            var normalized = NormalizePhone(phoneNumber);

            var customer = await _customerRepository.GetByPhoneAsync(
                _settings.BusinessId,
                normalized,
                cancellationToken);

            return customer == null ? null : _mapper.Map<CustomerResponse>(customer);
        }

        public async Task<CustomerResponse> CreateAsync(
            CreateCustomerRequest request,
            CancellationToken cancellationToken = default)
        {
            var normalized = NormalizePhone(request.PhoneNumber);

            var existing = await _customerRepository.GetByPhoneAsync(
                _settings.BusinessId,
                normalized,
                cancellationToken);

            if (existing != null)
                throw new ConflictException(DuplicatePhoneMessage);

            var customer = new Customer
            {
                BusinessId = _settings.BusinessId,
                PhoneNumber = normalized,
                FullName = Clean(request.FullName),
                Email = Clean(request.Email),
                CustomerType = request.CustomerType,
                Notes = Clean(request.Notes)
            };

            await _customerRepository.AddAsync(customer, cancellationToken);

            try
            {
                await _customerRepository.SaveChangesAsync(cancellationToken);
            }
            catch (DuplicateKeyException)
            {
                throw new ConflictException(DuplicatePhoneMessage);
            }

            _logger.LogInformation("Customer {CustomerId} created", customer.Id);

            return _mapper.Map<CustomerResponse>(customer);
        }

        public async Task<bool> UpdateAsync(
            int id,
            UpdateCustomerRequest request,
            CancellationToken cancellationToken = default)
        {
            var customer = await GetOwnCustomerAsync(id, cancellationToken);

            if (customer == null)
                return false;

            customer.FullName = Clean(request.FullName);
            customer.Email = Clean(request.Email);
            customer.CustomerType = request.CustomerType;
            customer.Notes = Clean(request.Notes);

            await _customerRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Customer {CustomerId} updated", id);

            return true;
        }

        public async Task<List<AppointmentResponse>> GetAppointmentsAsync(
            int customerId,
            CancellationToken cancellationToken = default)
        {
            var customer = await GetOwnCustomerAsync(customerId, cancellationToken)
                           ?? throw new NotFoundException($"לקוח {customerId} לא נמצא.");

            var business = await _settingsRepository.GetBusinessAsync(_settings.BusinessId, cancellationToken)
                           ?? throw new InvalidOperationException($"Business {_settings.BusinessId} is not configured.");

            var timeZone = BusinessTime.ResolveTimeZone(business.TimeZoneId);

            var appointments = await _appointmentRepository.GetByCustomerAsync(customerId, cancellationToken);

            return appointments
                .Select(a => AppointmentMapper.ToResponse(a, customer, a.AppointmentType, timeZone))
                .ToList();
        }

        private async Task<Customer?> GetOwnCustomerAsync(int id, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);

            return customer != null && customer.BusinessId == _settings.BusinessId ? customer : null;
        }

        private static string NormalizePhone(string phoneNumber)
        {
            if (!PhoneNumber.TryNormalize(phoneNumber, out var normalized))
                throw new BusinessRuleException(InvalidPhoneMessage);

            return normalized;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
