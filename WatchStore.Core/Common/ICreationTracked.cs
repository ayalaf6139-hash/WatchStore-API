namespace WatchStore.Core.Common
{
    public interface ICreationTracked
    {
        DateTime CreatedAtUtc { get; set; }

        string CreatedBy { get; set; }
    }
}
