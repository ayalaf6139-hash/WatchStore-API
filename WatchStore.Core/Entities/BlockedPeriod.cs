using WatchStore.Core.Common;

namespace WatchStore.Core.Entities
{
    public class BlockedPeriod : IAuditable
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }

        public int? AppointmentResourceId { get; set; }

        public DateTime StartUtc { get; set; }

        public DateTime EndUtc { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedBy { get; set; }

        public Business Business { get; set; } = null!;

        public AppointmentResource? AppointmentResource { get; set; }
    }
}
