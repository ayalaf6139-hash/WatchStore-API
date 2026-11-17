using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WatchStore.Core.Common;
using WatchStore.Core.DTOs;
using WatchStore.Core.Enums;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;
using WatchStore.Data.Interceptors;
using WatchStore.Data.Repositories;
using WatchStore.Service.Profiles;
using WatchStore.Service.Services;

namespace WatchStore.Tests.Scheduling
{
    public class SchedulingIntegrationTests : IAsyncLifetime
    {
        private const int ConsultationTypeId = 1;

        private static readonly DateTimeOffset Now = new(2027, 3, 1, 6, 0, 0, TimeSpan.Zero);

        private static readonly DateOnly Sunday = new(2027, 3, 7);

        private static readonly DateTime SundayTenLocalUtc = new(2027, 3, 7, 8, 0, 0, DateTimeKind.Utc);

        private static readonly IMapper Mapper = new MapperConfiguration(cfg =>
                cfg.AddMaps(typeof(ProductProfile).Assembly))
            .CreateMapper();

        private readonly WatchFlowSettings _settings = new();

        private readonly string _connectionString =
            (Environment.GetEnvironmentVariable("WATCHSTORE_TEST_POSTGRES")
             ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres") +
            $";Database=watchstore_scheduling_test_{Guid.NewGuid():N}";

        public async Task InitializeAsync()
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }

        [Fact]
        public async Task ScenarioA_NewCustomerBooksAvailableSlot()
        {
            await using var scope = CreateScope();

            var customer = await scope.Customers.CreateAsync(new CreateCustomerRequest
            {
                PhoneNumber = "050-7654321",
                FullName = "ישראל ישראלי",
                CustomerType = CustomerType.Collector
            });

            var availability = await scope.Scheduling.GetAvailabilityAsync(new AvailabilityQuery
            {
                AppointmentTypeId = ConsultationTypeId,
                FromDate = Sunday,
                ToDate = Sunday,
                CustomerType = CustomerType.Collector
            });

            Assert.Equal(45, availability.DurationMinutes);
            Assert.Equal("Asia/Jerusalem", availability.TimeZoneId);
            Assert.NotEmpty(availability.Slots);
            Assert.Equal(9, availability.Slots[0].StartLocal.Hour);

            var booked = await scope.Scheduling.BookAsync(new BookAppointmentRequest
            {
                CustomerId = customer.Id,
                AppointmentTypeId = ConsultationTypeId,
                StartUtc = availability.Slots[0].StartUtc,
                CustomerNotes = "מחפש שעון צלילה עד 5,000 ש\"ח"
            });

            Assert.Equal(AppointmentStatus.Booked, booked.Status);
            Assert.Equal(45, booked.DurationMinutes);
            Assert.Equal("+972507654321", booked.CustomerPhoneNumber);
            Assert.Equal(availability.Slots[0].StartUtc.AddMinutes(45), booked.EndUtc);

            await using var verify = CreateScope();

            var stored = await verify.Context.Appointments.AsNoTracking().SingleAsync();
            Assert.Equal(booked.EndUtc.AddMinutes(10), stored.ReservedEndUtc);

            var calendar = await verify.Appointments.GetPagedAsync(new AppointmentQueryParameters
            {
                FromDate = Sunday,
                ToDate = Sunday
            });

            Assert.Equal(1, calendar.TotalCount);
            Assert.Equal(booked.Id, calendar.Items[0].Id);
        }

        [Fact]
        public async Task ScenarioB_ExistingCustomerFoundByAnyPhoneFormat()
        {
            await using var scope = CreateScope();

            var created = await scope.Customers.CreateAsync(new CreateCustomerRequest { PhoneNumber = "050-1234567" });

            var found = await scope.Customers.FindByPhoneAsync("+972 50 123 4567");

            Assert.NotNull(found);
            Assert.Equal(created.Id, found!.Id);
        }

        [Fact]
        public async Task DuplicatePhone_ThrowsConflict()
        {
            await using var scope = CreateScope();

            await scope.Customers.CreateAsync(new CreateCustomerRequest { PhoneNumber = "0501234567" });

            await Assert.ThrowsAsync<ConflictException>(
                () => scope.Customers.CreateAsync(new CreateCustomerRequest { PhoneNumber = "+972501234567" }));
        }

        [Fact]
        public async Task InvalidPhone_ThrowsBusinessRuleException()
        {
            await using var scope = CreateScope();

            await Assert.ThrowsAsync<BusinessRuleException>(
                () => scope.Customers.FindByPhoneAsync("12"));
        }

        [Fact]
        public async Task ScenarioC_ConcurrentBookingsForSameSlot_OnlyOneSucceeds()
        {
            var firstCustomer = await CreateCustomerAsync("050-1111111");
            var secondCustomer = await CreateCustomerAsync("050-2222222");

            var outcomes = await Task.WhenAll(
                TryBookAsync(firstCustomer, SundayTenLocalUtc),
                TryBookAsync(secondCustomer, SundayTenLocalUtc));

            Assert.Single(outcomes, o => o.Appointment != null);

            var loser = outcomes.Single(o => o.Conflict != null).Conflict!;
            Assert.Equal(3, loser.Alternatives.Count);
            Assert.DoesNotContain(loser.Alternatives, a => a.StartUtc == SundayTenLocalUtc);

            await using var verify = CreateScope();
            Assert.Equal(1, await verify.Context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Booked));
        }

        [Fact]
        public async Task IdempotentRetry_ReturnsSameAppointment()
        {
            var customerId = await CreateCustomerAsync("050-3333333");
            var request = new BookAppointmentRequest
            {
                CustomerId = customerId,
                AppointmentTypeId = ConsultationTypeId,
                StartUtc = SundayTenLocalUtc,
                IdempotencyKey = "conversation-7-booking-1"
            };

            AppointmentResponse first;
            await using (var scope = CreateScope())
            {
                first = await scope.Scheduling.BookAsync(request);
            }

            await using var retry = CreateScope();
            var second = await retry.Scheduling.BookAsync(request);

            Assert.Equal(first.Id, second.Id);
            Assert.Equal(1, await retry.Context.Appointments.CountAsync());
        }

        [Fact]
        public async Task IdempotencyKeyReusedForDifferentSlot_ThrowsConflict()
        {
            var customerId = await CreateCustomerAsync("050-4444444");

            await using var scope = CreateScope();

            await scope.Scheduling.BookAsync(new BookAppointmentRequest
            {
                CustomerId = customerId,
                AppointmentTypeId = ConsultationTypeId,
                StartUtc = SundayTenLocalUtc,
                IdempotencyKey = "same-key"
            });

            await Assert.ThrowsAsync<ConflictException>(() => scope.Scheduling.BookAsync(new BookAppointmentRequest
            {
                CustomerId = customerId,
                AppointmentTypeId = ConsultationTypeId,
                StartUtc = SundayTenLocalUtc.AddHours(2),
                IdempotencyKey = "same-key"
            }));
        }

        [Fact]
        public async Task BookingWhenClosed_ReturnsAlternatives()
        {
            var customerId = await CreateCustomerAsync("050-5555555");
            var saturdayUtc = new DateTime(2027, 3, 13, 8, 0, 0, DateTimeKind.Utc);

            await using var scope = CreateScope();

            var conflict = await Assert.ThrowsAsync<SlotUnavailableException>(() => scope.Scheduling.BookAsync(
                new BookAppointmentRequest
                {
                    CustomerId = customerId,
                    AppointmentTypeId = ConsultationTypeId,
                    StartUtc = saturdayUtc
                }));

            Assert.Equal(3, conflict.Alternatives.Count);
            Assert.Equal(new DateOnly(2027, 3, 14), DateOnly.FromDateTime(conflict.Alternatives[0].StartLocal.DateTime));
        }

        [Fact]
        public async Task BookingInThePast_IsRejected()
        {
            var customerId = await CreateCustomerAsync("050-6666666");

            await using var scope = CreateScope();

            await Assert.ThrowsAsync<SlotUnavailableException>(() => scope.Scheduling.BookAsync(
                new BookAppointmentRequest
                {
                    CustomerId = customerId,
                    AppointmentTypeId = ConsultationTypeId,
                    StartUtc = new DateTime(2027, 2, 28, 8, 0, 0, DateTimeKind.Utc)
                }));
        }

        [Fact]
        public async Task StartTimeWithoutUtcMarker_IsRejected()
        {
            var customerId = await CreateCustomerAsync("050-7777777");

            await using var scope = CreateScope();

            await Assert.ThrowsAsync<BusinessRuleException>(() => scope.Scheduling.BookAsync(
                new BookAppointmentRequest
                {
                    CustomerId = customerId,
                    AppointmentTypeId = ConsultationTypeId,
                    StartUtc = new DateTime(2027, 3, 7, 8, 0, 0, DateTimeKind.Unspecified)
                }));
        }

        [Fact]
        public async Task CancelledAppointment_FreesTheSlot()
        {
            var firstCustomer = await CreateCustomerAsync("050-8888888");
            var secondCustomer = await CreateCustomerAsync("050-9999999");

            await using var scope = CreateScope();

            var first = await scope.Scheduling.BookAsync(new BookAppointmentRequest
            {
                CustomerId = firstCustomer,
                AppointmentTypeId = ConsultationTypeId,
                StartUtc = SundayTenLocalUtc
            });

            Assert.True(await scope.Appointments.CancelAsync(first.Id, new CancelAppointmentRequest { Reason = "הלקוח ביטל" }));

            await Assert.ThrowsAsync<BusinessRuleException>(
                () => scope.Appointments.CancelAsync(first.Id, new CancelAppointmentRequest()));

            await using var other = CreateScope();

            var second = await other.Scheduling.BookAsync(new BookAppointmentRequest
            {
                CustomerId = secondCustomer,
                AppointmentTypeId = ConsultationTypeId,
                StartUtc = SundayTenLocalUtc
            });

            Assert.Equal(SundayTenLocalUtc, second.StartUtc);
        }

        private async Task<int> CreateCustomerAsync(string phone)
        {
            await using var scope = CreateScope();

            var customer = await scope.Customers.CreateAsync(new CreateCustomerRequest { PhoneNumber = phone });

            return customer.Id;
        }

        private async Task<(AppointmentResponse? Appointment, SlotUnavailableException? Conflict)> TryBookAsync(
            int customerId,
            DateTime startUtc)
        {
            await using var scope = CreateScope();

            try
            {
                var appointment = await scope.Scheduling.BookAsync(new BookAppointmentRequest
                {
                    CustomerId = customerId,
                    AppointmentTypeId = ConsultationTypeId,
                    StartUtc = startUtc
                });

                return (appointment, null);
            }
            catch (SlotUnavailableException ex)
            {
                return (null, ex);
            }
        }

        private TestScope CreateScope()
        {
            var context = CreateContext();
            var timeProvider = new FixedTimeProvider(Now);
            var customers = new CustomerRepository(context);
            var appointments = new AppointmentRepository(context);
            var settings = new BusinessSettingsRepository(context);

            return new TestScope(
                context,
                new CustomerService(customers, appointments, settings, _settings, Mapper, NullLogger<CustomerService>.Instance),
                new SchedulingService(settings, appointments, customers, _settings, timeProvider, NullLogger<SchedulingService>.Instance),
                new AppointmentService(appointments, settings, _settings, timeProvider, NullLogger<AppointmentService>.Instance));
        }

        private WatchStoreDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<WatchStoreDbContext>()
                .UseNpgsql(_connectionString)
                .AddInterceptors(new AuditInterceptor(new FixedActor(), new FixedTimeProvider(Now)))
                .Options;

            return new WatchStoreDbContext(options);
        }

        private sealed record TestScope(
            WatchStoreDbContext Context,
            CustomerService Customers,
            SchedulingService Scheduling,
            AppointmentService Appointments) : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => Context.DisposeAsync();
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;

            public FixedTimeProvider(DateTimeOffset now)
            {
                _now = now;
            }

            public override DateTimeOffset GetUtcNow() => _now;
        }

        private sealed class FixedActor : ICurrentActor
        {
            public string ActorId => "integration-test";
        }
    }
}
