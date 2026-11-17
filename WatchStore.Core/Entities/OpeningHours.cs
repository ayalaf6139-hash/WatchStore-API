using WatchStore.Core.Common;

namespace WatchStore.Core.Entities
{
    public class OpeningHours : IAuditable
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeOnly OpenTime { get; set; }

        public TimeOnly CloseTime { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedBy { get; set; }

        public Business Business { get; set; } = null!;
    }
}
