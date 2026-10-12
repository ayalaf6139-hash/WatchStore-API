namespace WatchStore.Core.Exceptions
{
    // המשאב המבוקש לא קיים -> 404
    public class NotFoundException : Exception
    {
        public NotFoundException(string message)
            : base(message)
        {
        }
    }
}
