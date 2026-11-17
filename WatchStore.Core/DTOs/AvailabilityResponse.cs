namespace WatchStore.Core.DTOs
{
    public class AvailabilityResponse
    {
        public int AppointmentTypeId { get; set; }

        public int DurationMinutes { get; set; }

        public string TimeZoneId { get; set; } = string.Empty;

        public List<AvailableSlotResponse> Slots { get; set; } = new();
    }
}
