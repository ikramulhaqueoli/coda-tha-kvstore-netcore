namespace KvStore.Core.Domain.Exceptions;

public sealed class KeyValueNotFoundException(string key)
    : Exception($"Key '{key}' was not found.")
{
    public string Key { get; } = key;
}

