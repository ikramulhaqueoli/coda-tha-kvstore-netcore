namespace KvStore.Core.Domain.Exceptions;

public sealed class KeyValueNotFoundException : Exception
{
    public KeyValueNotFoundException(string key)
        : base($"Key '{key}' was not found.")
    {
        Key = key;
    }

    public string Key { get; }
}

