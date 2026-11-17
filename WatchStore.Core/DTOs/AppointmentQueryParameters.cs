using System.ComponentModel.DataAnnotations;
using WatchStore.Core.Enums;

namespace WatchStore.Core.DTOs
{
    public class AppointmentQueryParameters
    {
        public DateOnly? FromDate { get; set; }

        public DateOnly? ToDate { get; set; }

        public AppointmentStatus? Status { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }
}
