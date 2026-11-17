using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Core.Enums;

namespace WatchStore.Data.Seed
{
    public static class WatchFlowSeedData
    {
        public const int DefaultBusinessId = 1;

        public const int DefaultResourceId = 1;

        private const string SeedActor = "seed";

        private static readonly DateTime SeedDate =
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static void SeedWatchFlow(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Business>().HasData(new Business
            {
                Id = DefaultBusinessId,
                Name = "Friedman Watches",
                TimeZoneId = "Asia/Jerusalem",
                IsActive = true,
                CreatedAtUtc = SeedDate,
                CreatedBy = SeedActor
            });

            modelBuilder.Entity<AppointmentResource>().HasData(new AppointmentResource
            {
                Id = DefaultResourceId,
                BusinessId = DefaultBusinessId,
                Name = "עמדת מנהל",
                IsActive = true,
                CreatedAtUtc = SeedDate,
                CreatedBy = SeedActor
            });

            modelBuilder.Entity<AppointmentType>().HasData(
                CreateType(1, "ייעוץ ובחירת שעון", 30, 0, 10),
                CreateType(2, "רכישת שעון יוקרה", 60, 15, 10),
                CreateType(3, "מסירת שעון לתיקון", 15, 0, 5),
                CreateType(4, "התאמת רצועה", 15, 0, 5));

            modelBuilder.Entity<AppointmentDurationRule>().HasData(
                CreateRule(1, 1, CustomerType.Collector, null, 15, "אספן - שיחת ייעוץ מעמיקה"),
                CreateRule(2, 1, CustomerType.Corporate, null, 30, "לקוח מוסדי - כמה דגמים במקביל"),
                CreateRule(3, 2, null, true, 15, "הכנת דגמים מיוחדים לפני הפגישה"));

            var hours = new List<OpeningHours>();
            var id = 1;

            foreach (var day in new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday })
            {
                hours.Add(CreateHours(id++, day, new TimeOnly(9, 0), new TimeOnly(13, 0)));
                hours.Add(CreateHours(id++, day, new TimeOnly(14, 0), new TimeOnly(19, 0)));
            }

            hours.Add(CreateHours(id, DayOfWeek.Friday, new TimeOnly(9, 0), new TimeOnly(12, 30)));

            modelBuilder.Entity<OpeningHours>().HasData(hours);
        }

        private static AppointmentType CreateType(
            int id,
            string name,
            int baseMinutes,
            int bufferBefore,
            int bufferAfter)
        {
            return new AppointmentType
            {
                Id = id,
                BusinessId = DefaultBusinessId,
                Name = name,
                BaseDurationMinutes = baseMinutes,
                BufferBeforeMinutes = bufferBefore,
                BufferAfterMinutes = bufferAfter,
                IsActive = true,
                CreatedAtUtc = SeedDate,
                CreatedBy = SeedActor
            };
        }

        private static AppointmentDurationRule CreateRule(
            int id,
            int appointmentTypeId,
            CustomerType? customerType,
            bool? requiresSpecialPreparation,
            int extraMinutes,
            string description)
        {
            return new AppointmentDurationRule
            {
                Id = id,
                AppointmentTypeId = appointmentTypeId,
                CustomerType = customerType,
                RequiresSpecialPreparation = requiresSpecialPreparation,
                ExtraMinutes = extraMinutes,
                Description = description,
                CreatedAtUtc = SeedDate,
                CreatedBy = SeedActor
            };
        }

        private static OpeningHours CreateHours(int id, DayOfWeek day, TimeOnly open, TimeOnly close)
        {
            return new OpeningHours
            {
                Id = id,
                BusinessId = DefaultBusinessId,
                DayOfWeek = day,
                OpenTime = open,
                CloseTime = close,
                CreatedAtUtc = SeedDate,
                CreatedBy = SeedActor
            };
        }
    }
}
