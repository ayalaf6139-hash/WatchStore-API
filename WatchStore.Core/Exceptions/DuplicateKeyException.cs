namespace WatchStore.Core.Exceptions
{
    public class DuplicateKeyException : Exception
    {
        public DuplicateKeyException(string keyName, Exception innerException)
            : base($"A record with the same value for '{keyName}' already exists.", innerException)
        {
            KeyName = keyName;
        }

        public string KeyName { get; }
    }
}
