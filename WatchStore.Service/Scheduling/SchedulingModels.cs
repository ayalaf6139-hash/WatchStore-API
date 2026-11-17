using WatchStore.Core.Entities;

namespace WatchStore.Service.Scheduling
{
    public sealed record AppointmentDuration(
        int DurationMinutes,
        int BufferBeforeMinutes,
        int BufferAfterMinutes);

    public sealed record BusyPeriod(
        int? ResourceId,
        DateTime StartUtc,
        DateTime EndUtc);

    public sealed record CandidateSlot(
        int ResourceId,
        DateTime StartUtc,
        DateTime EndUtc,
        DateTime ReservedStartUtc,
        DateTime ReservedEndUtc);

    public sealed class SlotSearch
    {
        public required TimeZoneInfo TimeZone { get; init; }

        public required DateOnly FromDate { get; init; }

        public required DateOnly ToDate { get; init; }

        public required IReadOnlyList<OpeningHours> OpeningHours { get; init; }

        public required IReadOnlyList<int> ResourceIds { get; init; }

        public required IReadOnlyList<BusyPeriod> BusyPeriods { get; init; }

        public required AppointmentDuration Duration { get; init; }

        public required int SlotIntervalMinutes { get; init; }

        public required DateTime EarliestStartUtc { get; init; }

        public int MaxResults { get; init; } = int.MaxValue;
    }
}
