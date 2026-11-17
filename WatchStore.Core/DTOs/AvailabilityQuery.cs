using System.ComponentModel.DataAnnotations;
using WatchStore.Core.Enums;

namespace WatchStore.Core.DTOs
{
    public class AvailabilityQuery
    {
        [Range(1, int.MaxValue)]
        public int AppointmentTypeId { get; set; }

        public DateOnly FromDate { get; set; }

        public DateOnly? ToDate { get; set; }

        public CustomerType CustomerType { get; set; } = CustomerType.Private;

        public bool RequiresSpecialPreparation { get; set; }

        [Range(1, 100)]
        public int MaxResults { get; set; } = 20;
    }
}
