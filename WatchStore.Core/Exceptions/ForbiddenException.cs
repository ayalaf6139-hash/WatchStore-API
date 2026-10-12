namespace WatchStore.Core.Exceptions
{
    // המשתמש מחובר, אבל אין לו הרשאה למשאב הספציפי -> 403
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message)
            : base(message)
        {
        }
    }
}
