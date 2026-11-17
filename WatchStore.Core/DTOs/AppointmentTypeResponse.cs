namespace WatchStore.Core.DTOs
{
    public class AppointmentTypeResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int BaseDurationMinutes { get; set; }

        public int BufferBeforeMinutes { get; set; }

        public int BufferAfterMinutes { get; set; }
    }
}
