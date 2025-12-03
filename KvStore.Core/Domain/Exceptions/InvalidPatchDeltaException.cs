namespace KvStore.Core.Domain.Exceptions;

public sealed class InvalidPatchDeltaException : Exception
{
    public InvalidPatchDeltaException() : base("PATCH delta must not be null.")
    {
    }
}

