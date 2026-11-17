using WatchStore.Core.Entities;
using WatchStore.Core.Enums;
using WatchStore.Core.Exceptions;
using WatchStore.Service.Scheduling;

namespace WatchStore.Tests.Scheduling
{
    public class AppointmentDurationCalculatorTests
    {
        private static AppointmentType CreateType(params AppointmentDurationRule[] rules)
        {
            return new AppointmentType
            {
                Id = 1,
                Name = "ייעוץ",
                BaseDurationMinutes = 30,
                BufferBeforeMinutes = 5,
                BufferAfterMinutes = 10,
                DurationRules = rules.ToList()
            };
        }

        [Fact]
        public void NoRules_ReturnsBaseDurationAndBuffers()
        {
            var result = AppointmentDurationCalculator.Calculate(CreateType(), CustomerType.Private, false);

            Assert.Equal(new AppointmentDuration(30, 5, 10), result);
        }

        [Fact]
        public void CustomerTypeRule_AppliesOnlyToMatchingCustomer()
        {
            var type = CreateType(new AppointmentDurationRule { CustomerType = CustomerType.Collector, ExtraMinutes = 15 });

            Assert.Equal(45, AppointmentDurationCalculator.Calculate(type, CustomerType.Collector, false).DurationMinutes);
            Assert.Equal(30, AppointmentDurationCalculator.Calculate(type, CustomerType.Private, false).DurationMinutes);
        }

        [Fact]
        public void MatchingRules_AreSummed()
        {
            var type = CreateType(
                new AppointmentDurationRule { CustomerType = CustomerType.Corporate, ExtraMinutes = 30 },
                new AppointmentDurationRule { RequiresSpecialPreparation = true, ExtraMinutes = 15 },
                new AppointmentDurationRule { RequiresSpecialPreparation = false, ExtraMinutes = 100 });

            var result = AppointmentDurationCalculator.Calculate(type, CustomerType.Corporate, true);

            Assert.Equal(75, result.DurationMinutes);
        }

        [Fact]
        public void NonPositiveResult_ThrowsBusinessRuleException()
        {
            var type = CreateType(new AppointmentDurationRule { ExtraMinutes = -30 });

            Assert.Throws<BusinessRuleException>(
                () => AppointmentDurationCalculator.Calculate(type, CustomerType.Private, false));
        }
    }
}
