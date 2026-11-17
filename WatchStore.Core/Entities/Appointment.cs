using WatchStore.Core.Common;
using WatchStore.Core.Enums;

namespace WatchStore.Core.Entities
{
    public class Appointment : IAuditable
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }

        public int CustomerId { get; set; }

        public int AppointmentTypeId { get; set; }

        public int AppointmentResourceId { get; set; }

        public int? ConversationId { get; set; }

        public DateTime StartUtc { get; set; }

        public DateTime EndUtc { get; set; }

        public DateTime ReservedStartUtc { get; set; }

        public DateTime ReservedEndUtc { get; set; }

        public int DurationMinutes { get; set; }

        public AppointmentStatus Status { get; set; }

        public string? IdempotencyKey { get; set; }

        public string? CustomerNotes { get; set; }

        public string? PreparationNotes { get; set; }

        public DateTime? CancelledAtUtc { get; set; }

        public string? CancellationReason { get; set; }

        public uint RowVersion { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedBy { get; set; }

        public Business Business { get; set; } = null!;

        public Customer Customer { get; set; } = null!;

        public AppointmentType AppointmentType { get; set; } = null!;

        public AppointmentResource AppointmentResource { get; set; } = null!;

        public Conversation? Conversation { get; set; }
    }
}
