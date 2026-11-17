using WatchStore.Core.Entities;
using WatchStore.Core.Enums;
using WatchStore.Core.Exceptions;

namespace WatchStore.Service.Scheduling
{
    public static class AppointmentDurationCalculator
    {
        public static AppointmentDuration Calculate(
            AppointmentType appointmentType,
            CustomerType customerType,
            bool requiresSpecialPreparation)
        {
            var extraMinutes = appointmentType.DurationRules
                .Where(rule => Matches(rule, customerType, requiresSpecialPreparation))
                .Sum(rule => rule.ExtraMinutes);

            var durationMinutes = appointmentType.BaseDurationMinutes + extraMinutes;

            if (durationMinutes <= 0)
            {
                throw new BusinessRuleException(
                    "משך התור שחושב אינו תקין. יש לבדוק את כללי המשך של סוג התור.");
            }

            return new AppointmentDuration(
                durationMinutes,
                appointmentType.BufferBeforeMinutes,
                appointmentType.BufferAfterMinutes);
        }

        private static bool Matches(
            AppointmentDurationRule rule,
            CustomerType customerType,
            bool requiresSpecialPreparation)
        {
            return (rule.CustomerType == null || rule.CustomerType == customerType) &&
                   (rule.RequiresSpecialPreparation == null || rule.RequiresSpecialPreparation == requiresSpecialPreparation);
        }
    }
}
