namespace WatchStore.Core.Exceptions
{
    // נזרקת מה-Repository כשה-RowVersion לא תאם בזמן השמירה:
    // מישהו אחר שינה את השורה בין הקריאה לשמירה.
    // עוטפת את DbUpdateConcurrencyException כדי ששכבת ה-Service לא תהיה תלויה ב-EF Core
    public class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
