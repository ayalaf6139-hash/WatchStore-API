using WatchStore.Core.Common;

namespace WatchStore.Core.Entities
{
    public class AppointmentResource : IAuditable
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedBy { get; set; }

        public Business Business { get; set; } = null!;
    }
}
