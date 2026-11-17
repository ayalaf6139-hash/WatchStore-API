using WatchStore.Core.Common;

namespace WatchStore.Core.Entities
{
    public class Business : IAuditable
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string TimeZoneId { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedBy { get; set; }
    }
}
