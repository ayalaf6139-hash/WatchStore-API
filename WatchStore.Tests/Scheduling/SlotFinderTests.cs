using WatchStore.Core.Entities;
using WatchStore.Service.Scheduling;

namespace WatchStore.Tests.Scheduling
{
    public class SlotFinderTests
    {
        private static readonly TimeZoneInfo Jerusalem = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

        private static readonly DateOnly Sunday = new DateOnly(2027, 3, 7);

        private static readonly List<OpeningHours> SundayHours = new()
        {
            new OpeningHours { DayOfWeek = DayOfWeek.Sunday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(13, 0) },
            new OpeningHours { DayOfWeek = DayOfWeek.Sunday, OpenTime = new TimeOnly(14, 0), CloseTime = new TimeOnly(19, 0) }
        };

        private static DateTime Utc(int month, int day, int hour, int minute = 0)
        {
            return new DateTime(2027, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        private static List<CandidateSlot> Find(
            DateOnly? date = null,
            IReadOnlyList<int>? resources = null,
            IReadOnlyList<BusyPeriod>? busy = null,
            AppointmentDuration? duration = null,
            DateTime? earliest = null,
            int maxResults = int.MaxValue)
        {
            return SlotFinder.FindSlots(new SlotSearch
            {
                TimeZone = Jerusalem,
                FromDate = date ?? Sunday,
                ToDate = date ?? Sunday,
                OpeningHours = SundayHours,
                ResourceIds = resources ?? new[] { 1 },
                BusyPeriods = busy ?? Array.Empty<BusyPeriod>(),
                Duration = duration ?? new AppointmentDuration(30, 0, 0),
                SlotIntervalMinutes = 15,
                EarliestStartUtc = earliest ?? DateTime.MinValue,
                MaxResults = maxResults
            });
        }

        [Fact]
        public void EmptyDay_ReturnsAllSlotsInBothWindows()
        {
            var slots = Find();

            Assert.Equal(34, slots.Count);
            Assert.Equal(Utc(3, 7, 7), slots[0].StartUtc);
            Assert.Equal(Utc(3, 7, 7, 30), slots[0].EndUtc);
        }

        [Fact]
        public void SlotsNeverCrossTheBreakOrClosingTime()
        {
            var slots = Find();

            Assert.Contains(slots, s => s.StartUtc == Utc(3, 7, 10, 30));
            Assert.DoesNotContain(slots, s => s.StartUtc == Utc(3, 7, 10, 45));
            Assert.DoesNotContain(slots, s => s.StartUtc == Utc(3, 7, 11, 0));
            Assert.Equal(Utc(3, 7, 16, 30), slots[^1].StartUtc);
        }

        [Fact]
        public void BookedAppointment_RemovesOverlappingSlotsOnly()
        {
            var busy = new[] { new BusyPeriod(1, Utc(3, 7, 8), Utc(3, 7, 8, 30)) };

            var starts = Find(busy: busy).Select(s => s.StartUtc).ToList();

            Assert.Contains(Utc(3, 7, 7, 30), starts);
            Assert.DoesNotContain(Utc(3, 7, 7, 45), starts);
            Assert.DoesNotContain(Utc(3, 7, 8), starts);
            Assert.DoesNotContain(Utc(3, 7, 8, 15), starts);
            Assert.Contains(Utc(3, 7, 8, 30), starts);
        }

        [Fact]
        public void PreparationBuffer_IsPartOfTheReservedRange()
        {
            var busy = new[] { new BusyPeriod(1, Utc(3, 7, 8), Utc(3, 7, 8, 30)) };

            var slots = Find(busy: busy, duration: new AppointmentDuration(30, 15, 0));
            var starts = slots.Select(s => s.StartUtc).ToList();

            Assert.DoesNotContain(Utc(3, 7, 8, 30), starts);
            Assert.Contains(Utc(3, 7, 8, 45), starts);

            var slot = slots.Single(s => s.StartUtc == Utc(3, 7, 8, 45));
            Assert.Equal(Utc(3, 7, 8, 30), slot.ReservedStartUtc);
        }

        [Fact]
        public void BusinessWideBlockedPeriod_BlocksAllResources()
        {
            var busy = new[] { new BusyPeriod(null, Utc(3, 7, 7), Utc(3, 7, 11)) };

            var slots = Find(resources: new[] { 1, 2 }, busy: busy);

            Assert.All(slots, s => Assert.True(s.StartUtc >= Utc(3, 7, 12)));
        }

        [Fact]
        public void SecondResource_IsUsedWhenFirstIsBusy()
        {
            var busy = new[] { new BusyPeriod(1, Utc(3, 7, 8), Utc(3, 7, 8, 30)) };

            var slot = Find(resources: new[] { 1, 2 }, busy: busy).Single(s => s.StartUtc == Utc(3, 7, 8));

            Assert.Equal(2, slot.ResourceId);
        }

        [Fact]
        public void EarliestStart_FiltersEarlierSlots()
        {
            var slots = Find(earliest: Utc(3, 7, 9, 5));

            Assert.Equal(Utc(3, 7, 9, 15), slots[0].StartUtc);
        }

        [Fact]
        public void ClosedDay_ReturnsNoSlots()
        {
            Assert.Empty(Find(date: new DateOnly(2027, 3, 13)));
        }

        [Fact]
        public void DaylightSavingTime_ShiftsUtcStart()
        {
            var afterDstChange = Find(date: new DateOnly(2027, 3, 28));

            Assert.Equal(Utc(3, 28, 6), afterDstChange[0].StartUtc);
        }

        [Fact]
        public void MaxResults_IsRespected()
        {
            Assert.Equal(5, Find(maxResults: 5).Count);
        }
    }
}
