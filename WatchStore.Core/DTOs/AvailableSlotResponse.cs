namespace WatchStore.Core.DTOs
{
    public class AvailableSlotResponse
    {
        public DateTime StartUtc { get; set; }

        public DateTime EndUtc { get; set; }

        public DateTimeOffset StartLocal { get; set; }

        public DateTimeOffset EndLocal { get; set; }

        public int DurationMinutes { get; set; }
    }
}
