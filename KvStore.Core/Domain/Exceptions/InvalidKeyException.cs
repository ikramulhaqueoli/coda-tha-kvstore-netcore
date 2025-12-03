namespace KvStore.Core.Domain.Exceptions;

public sealed class InvalidKeyException : Exception
{
    public InvalidKeyException(string key)
        : base("Key must be non-empty and contain only alphanumeric characters.")
    {
        Key = key;
    }

    public string Key { get; }
}

