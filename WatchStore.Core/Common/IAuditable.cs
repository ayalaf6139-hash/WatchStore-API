namespace WatchStore.Core.Common
{
    public interface IAuditable : ICreationTracked
    {
        DateTime? UpdatedAtUtc { get; set; }

        string? UpdatedBy { get; set; }
    }
}
