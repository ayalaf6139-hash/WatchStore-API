namespace WatchStore.Core.Exceptions
{
    public class AppointmentSlotConflictException : Exception
    {
        public AppointmentSlotConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
