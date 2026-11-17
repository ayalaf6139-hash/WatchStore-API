namespace WatchStore.Core.Common
{
    public class WatchFlowSettings
    {
        public const string SectionName = "WatchFlow";

        public int BusinessId { get; set; } = 1;

        public int SlotIntervalMinutes { get; set; } = 15;

        public int MinimumNoticeMinutes { get; set; } = 60;

        public int DefaultSearchDays { get; set; } = 7;

        public int MaxSearchDays { get; set; } = 14;

        public int AlternativesCount { get; set; } = 3;
    }
}
