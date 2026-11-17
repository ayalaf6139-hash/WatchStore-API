using WatchStore.Core.Enums;

namespace WatchStore.Core.DTOs
{
    public class AppointmentResponse
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public string CustomerPhoneNumber { get; set; } = string.Empty;

        public int AppointmentTypeId { get; set; }

        public string AppointmentTypeName { get; set; } = string.Empty;

        public DateTime StartUtc { get; set; }

        public DateTime EndUtc { get; set; }

        public DateTimeOffset StartLocal { get; set; }

        public DateTimeOffset EndLocal { get; set; }

        public int DurationMinutes { get; set; }

        public AppointmentStatus Status { get; set; }

        public int? ConversationId { get; set; }

        public string? CustomerNotes { get; set; }

        public string? PreparationNotes { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }
}
