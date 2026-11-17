using WatchStore.Core.Exceptions;

namespace WatchStore.Service.Scheduling
{
    public static class BusinessTime
    {
        public static TimeZoneInfo ResolveTimeZone(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                throw new InvalidOperationException($"Time zone '{timeZoneId}' is not available on this machine.", ex);
            }
        }

        public static DateTimeOffset ToLocal(DateTime utc, TimeZoneInfo timeZone)
        {
            var asUtc = new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
            return TimeZoneInfo.ConvertTime(asUtc, timeZone);
        }

        public static DateOnly ToLocalDate(DateTime utc, TimeZoneInfo timeZone)
        {
            return DateOnly.FromDateTime(ToLocal(utc, timeZone).DateTime);
        }

        public static DateTime StartOfLocalDayUtc(DateOnly date, TimeZoneInfo timeZone)
        {
            return TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), timeZone);
        }

        public static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => throw new BusinessRuleException(
                    "יש לשלוח את הזמן ב-UTC, עם Z בסוף (לדוגמה 2027-03-07T08:00:00Z).")
            };
        }
    }
}
