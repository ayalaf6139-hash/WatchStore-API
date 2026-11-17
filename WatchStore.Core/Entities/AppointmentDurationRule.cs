using WatchStore.Core.Common;
using WatchStore.Core.Enums;

namespace WatchStore.Core.Entities
{
    public class AppointmentDurationRule : IAuditable
    {
        public int Id { get; set; }

        public int AppointmentTypeId { get; set; }

        public CustomerType? CustomerType { get; set; }

        public bool? RequiresSpecialPreparation { get; set; }

        public int ExtraMinutes { get; set; }

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedBy { get; set; }

        public AppointmentType AppointmentType { get; set; } = null!;
    }
}
