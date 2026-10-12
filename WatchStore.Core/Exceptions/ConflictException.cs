namespace WatchStore.Core.Exceptions
{
    // הבקשה תקינה, אבל מתנגשת עם המצב הנוכחי של הנתונים -> 409
    public class ConflictException : Exception
    {
        public ConflictException(string message)
            : base(message)
        {
        }
    }
}
