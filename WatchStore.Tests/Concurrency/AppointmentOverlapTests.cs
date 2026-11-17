using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Core.Enums;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;
using WatchStore.Data.Interceptors;
using WatchStore.Data.Repositories;
using WatchStore.Data.Seed;

namespace WatchStore.Tests.Concurrency
{
    public class AppointmentOverlapTests : IAsyncLifetime
    {
        private const string TestActor = "test-actor";

        private static readonly DateTime Day =
            new DateTime(2027, 3, 7, 0, 0, 0, DateTimeKind.Utc);

        private readonly string _connectionString =
            (Environment.GetEnvironmentVariable("WATCHSTORE_TEST_POSTGRES")
             ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres") +
            $";Database=watchstore_appointments_test_{Guid.NewGuid():N}";

        private int _customerId;

        public async Task InitializeAsync()
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();

            var customer = new Customer
            {
                BusinessId = WatchFlowSeedData.DefaultBusinessId,
                PhoneNumber = "+972501234567",
                FullName = "לקוח בדיקה",
                CustomerType = CustomerType.Private
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();
            _customerId = customer.Id;
        }

        public async Task DisposeAsync()
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }

        [Fact]
        public async Task OverlappingBooking_SecondSaveThrowsSlotConflict()
        {
            await using var contextA = CreateContext();
            await using var contextB = CreateContext();
            var repositoryA = new AppointmentRepository(contextA);
            var repositoryB = new AppointmentRepository(contextB);

            await repositoryA.AddAsync(NewAppointment(Day.AddHours(8), 30));
            await repositoryA.SaveChangesAsync();

            await repositoryB.AddAsync(NewAppointment(Day.AddHours(8).AddMinutes(15), 30));

            var exception = await Assert.ThrowsAsync<AppointmentSlotConflictException>(
                () => repositoryB.SaveChangesAsync());

            Assert.IsType<DbUpdateException>(exception.InnerException);
        }

        [Fact]
        public async Task BackToBackBookings_BothSucceed()
        {
            await using var context = CreateContext();
            var repository = new AppointmentRepository(context);

            await repository.AddAsync(NewAppointment(Day.AddHours(9), 30));
            await repository.SaveChangesAsync();

            await repository.AddAsync(NewAppointment(Day.AddHours(9).AddMinutes(30), 30));
            await repository.SaveChangesAsync();

            Assert.Equal(2, await CountBookedAsync());
        }

        [Fact]
        public async Task ConcurrentBookingsForSameSlot_OnlyOneSucceeds()
        {
            var start = Day.AddHours(10);

            var results = await Task.WhenAll(
                TryBookAsync(start),
                TryBookAsync(start),
                TryBookAsync(start));

            Assert.Equal(1, results.Count(succeeded => succeeded));
            Assert.Equal(1, await CountBookedAsync());
        }

        [Fact]
        public async Task CancelledAppointment_DoesNotBlockSlot()
        {
            await using var context = CreateContext();
            var repository = new AppointmentRepository(context);

            await repository.AddAsync(NewAppointment(Day.AddHours(11), 30, status: AppointmentStatus.Cancelled));
            await repository.SaveChangesAsync();

            await repository.AddAsync(NewAppointment(Day.AddHours(11), 30));
            await repository.SaveChangesAsync();

            Assert.Equal(1, await CountBookedAsync());
        }

        [Fact]
        public async Task BookingInsidePreparationBuffer_IsRejected()
        {
            await using var contextA = CreateContext();
            await using var contextB = CreateContext();
            var repositoryA = new AppointmentRepository(contextA);
            var repositoryB = new AppointmentRepository(contextB);

            await repositoryA.AddAsync(NewAppointment(Day.AddHours(12), 30));
            await repositoryA.SaveChangesAsync();

            await repositoryB.AddAsync(NewAppointment(Day.AddHours(12).AddMinutes(40), 30, bufferBefore: 15));

            await Assert.ThrowsAsync<AppointmentSlotConflictException>(
                () => repositoryB.SaveChangesAsync());
        }

        [Fact]
        public async Task DuplicateIdempotencyKey_ThrowsDuplicateKeyException()
        {
            await using var contextA = CreateContext();
            await using var contextB = CreateContext();
            var repositoryA = new AppointmentRepository(contextA);
            var repositoryB = new AppointmentRepository(contextB);

            await repositoryA.AddAsync(NewAppointment(Day.AddHours(14), 30, idempotencyKey: "call-42-slot-1"));
            await repositoryA.SaveChangesAsync();

            await repositoryB.AddAsync(NewAppointment(Day.AddHours(15), 30, idempotencyKey: "call-42-slot-1"));

            var exception = await Assert.ThrowsAsync<DuplicateKeyException>(
                () => repositoryB.SaveChangesAsync());

            Assert.Equal(DbConstraintNames.AppointmentIdempotencyKey, exception.KeyName);
        }

        [Fact]
        public async Task AuditInterceptor_FillsCreationFields()
        {
            await using var context = CreateContext();
            var repository = new AppointmentRepository(context);
            var appointment = NewAppointment(Day.AddHours(16), 30);

            await repository.AddAsync(appointment);
            await repository.SaveChangesAsync();

            Assert.Equal(TestActor, appointment.CreatedBy);
            Assert.NotEqual(default, appointment.CreatedAtUtc);
            Assert.Null(appointment.UpdatedAtUtc);
        }

        private async Task<bool> TryBookAsync(DateTime startUtc)
        {
            await using var context = CreateContext();
            var repository = new AppointmentRepository(context);

            await repository.AddAsync(NewAppointment(startUtc, 30));

            try
            {
                await repository.SaveChangesAsync();
                return true;
            }
            catch (AppointmentSlotConflictException)
            {
                return false;
            }
        }

        private async Task<int> CountBookedAsync()
        {
            await using var context = CreateContext();

            return await context.Appointments
                .CountAsync(a => a.Status == AppointmentStatus.Booked);
        }

        private Appointment NewAppointment(
            DateTime startUtc,
            int minutes,
            int bufferBefore = 0,
            int bufferAfter = 0,
            AppointmentStatus status = AppointmentStatus.Booked,
            string? idempotencyKey = null)
        {
            var endUtc = startUtc.AddMinutes(minutes);

            return new Appointment
            {
                BusinessId = WatchFlowSeedData.DefaultBusinessId,
                CustomerId = _customerId,
                AppointmentTypeId = 1,
                AppointmentResourceId = WatchFlowSeedData.DefaultResourceId,
                StartUtc = startUtc,
                EndUtc = endUtc,
                ReservedStartUtc = startUtc.AddMinutes(-bufferBefore),
                ReservedEndUtc = endUtc.AddMinutes(bufferAfter),
                DurationMinutes = minutes,
                Status = status,
                IdempotencyKey = idempotencyKey
            };
        }

        private WatchStoreDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<WatchStoreDbContext>()
                .UseNpgsql(_connectionString)
                .AddInterceptors(new AuditInterceptor(new FixedActor(), TimeProvider.System))
                .Options;

            return new WatchStoreDbContext(options);
        }

        private sealed class FixedActor : ICurrentActor
        {
            public string ActorId => TestActor;
        }
    }
}
