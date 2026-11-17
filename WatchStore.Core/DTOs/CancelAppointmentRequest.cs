using System.ComponentModel.DataAnnotations;

namespace WatchStore.Core.DTOs
{
    public class CancelAppointmentRequest
    {
        [StringLength(500)]
        public string? Reason { get; set; }
    }
}
