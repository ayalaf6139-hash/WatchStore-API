namespace WatchStore.Service.Scheduling
{
    public static class SlotFinder
    {
        public static List<CandidateSlot> FindSlots(SlotSearch search)
        {
            if (search.SlotIntervalMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(search), "Slot interval must be positive.");

            var slots = new List<CandidateSlot>();
            var duration = search.Duration;

            for (var date = search.FromDate; date <= search.ToDate; date = date.AddDays(1))
            {
                var dayHours = search.OpeningHours
                    .Where(h => h.DayOfWeek == date.DayOfWeek)
                    .OrderBy(h => h.OpenTime);

                foreach (var hours in dayHours)
                {
                    var localClose = date.ToDateTime(hours.CloseTime);

                    for (var localStart = date.ToDateTime(hours.OpenTime);
                         localStart.AddMinutes(duration.DurationMinutes) <= localClose;
                         localStart = localStart.AddMinutes(search.SlotIntervalMinutes))
                    {
                        if (search.TimeZone.IsInvalidTime(localStart))
                            continue;

                        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, search.TimeZone);

                        if (startUtc < search.EarliestStartUtc)
                            continue;

                        var endUtc = startUtc.AddMinutes(duration.DurationMinutes);
                        var reservedStartUtc = startUtc.AddMinutes(-duration.BufferBeforeMinutes);
                        var reservedEndUtc = endUtc.AddMinutes(duration.BufferAfterMinutes);

                        var resourceId = FindFreeResource(search, reservedStartUtc, reservedEndUtc);

                        if (resourceId == null)
                            continue;

                        slots.Add(new CandidateSlot(
                            resourceId.Value,
                            startUtc,
                            endUtc,
                            reservedStartUtc,
                            reservedEndUtc));

                        if (slots.Count >= search.MaxResults)
                            return slots;
                    }
                }
            }

            return slots;
        }

        private static int? FindFreeResource(SlotSearch search, DateTime fromUtc, DateTime toUtc)
        {
            foreach (var resourceId in search.ResourceIds)
            {
                var isBusy = search.BusyPeriods.Any(period =>
                    (period.ResourceId == null || period.ResourceId == resourceId) &&
                    period.StartUtc < toUtc &&
                    period.EndUtc > fromUtc);

                if (!isBusy)
                    return resourceId;
            }

            return null;
        }
    }
}
