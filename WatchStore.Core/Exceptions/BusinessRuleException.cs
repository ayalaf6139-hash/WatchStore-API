namespace WatchStore.Core.Exceptions
{
    // הבקשה תקינה מבחינת מבנה, אבל נדחית בגלל כלל עסקי -> 400
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message)
            : base(message)
        {
        }
    }
}
