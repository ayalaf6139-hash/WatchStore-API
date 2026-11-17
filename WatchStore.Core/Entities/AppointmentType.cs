using WatchStore.Core.Common;

namespace WatchStore.Core.Entities
{
    public class AppointmentType : IAuditable
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int BaseDurationMinutes { get; set; }

        public int BufferBeforeMinutes { get; set; }

        public int BufferAfterMinutes { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedBy { get; set; }

        public Business Business { get; set; } = null!;

        public ICollection<AppointmentDurationRule> DurationRules { get; set; }
            = new List<AppointmentDurationRule>();
    }
}
