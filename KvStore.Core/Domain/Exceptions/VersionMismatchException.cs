namespace KvStore.Core.Domain.Exceptions;

public sealed class VersionMismatchException : Exception
{
    public VersionMismatchException(string key, int expectedVersion, int? currentVersion)
        : base($"Version mismatch for key '{key}'. Expected {expectedVersion}, actual {currentVersion?.ToString() ?? "null"}.")
    {
        Key = key;
        ExpectedVersion = expectedVersion;
        CurrentVersion = currentVersion;
    }

    public string Key { get; }
    public int ExpectedVersion { get; }
    public int? CurrentVersion { get; }
}

