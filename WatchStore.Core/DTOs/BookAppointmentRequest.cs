using System.ComponentModel.DataAnnotations;

namespace WatchStore.Core.DTOs
{
    public class BookAppointmentRequest
    {
        [Range(1, int.MaxValue)]
        public int CustomerId { get; set; }

        [Range(1, int.MaxValue)]
        public int AppointmentTypeId { get; set; }

        [Required]
        public DateTime StartUtc { get; set; }

        public int? ConversationId { get; set; }

        public bool RequiresSpecialPreparation { get; set; }

        [StringLength(100)]
        public string? IdempotencyKey { get; set; }

        [StringLength(1000)]
        public string? CustomerNotes { get; set; }

        [StringLength(2000)]
        public string? PreparationNotes { get; set; }
    }
}
