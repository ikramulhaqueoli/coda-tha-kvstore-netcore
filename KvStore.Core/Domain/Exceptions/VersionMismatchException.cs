namespace KvStore.Core.Domain.Exceptions;

public sealed class VersionMismatchException : Exception
{
    public VersionMismatchException(string key, long expectedVersion, long? currentVersion)
        : base($"Version mismatch for key '{key}'. Expected {expectedVersion}, actual {currentVersion?.ToString() ?? "null"}.")
    {
        Key = key;
        ExpectedVersion = expectedVersion;
        CurrentVersion = currentVersion;
    }

    public string Key { get; }
    public long ExpectedVersion { get; }
    public long? CurrentVersion { get; }
}

