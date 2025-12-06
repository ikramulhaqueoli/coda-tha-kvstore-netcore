using KvStore.Core.Domain.Exceptions;

namespace KvStore.UnitTest.Core.Domain.Exceptions;

public sealed class ExceptionTests
{
    [Fact]
    public void InvalidKeyException_ContainsKey()
    {
        var exception = new InvalidKeyException("test-key");

        Assert.Equal("test-key", exception.Key);
        Assert.Contains("alphanumeric", exception.Message);
    }

    [Fact]
    public void VersionMismatchException_ContainsAllProperties()
    {
        var exception = new VersionMismatchException("test-key", 5, 1);

        Assert.Equal("test-key", exception.Key);
        Assert.Equal(5, exception.ExpectedVersion);
        Assert.Equal(1, exception.CurrentVersion);
        Assert.Contains("test-key", exception.Message);
        Assert.Contains("5", exception.Message);
    }

    [Fact]
    public void VersionMismatchException_WithNullCurrentVersion()
    {
        var exception = new VersionMismatchException("test-key", 5, null);

        Assert.Equal("test-key", exception.Key);
        Assert.Equal(5, exception.ExpectedVersion);
        Assert.Null(exception.CurrentVersion);
        Assert.Contains("null", exception.Message);
    }

    [Fact]
    public void KeyValueNotFoundException_ContainsKey()
    {
        var exception = new KeyValueNotFoundException("missing-key");

        Assert.Equal("missing-key", exception.Key);
        Assert.Contains("missing-key", exception.Message);
    }

    [Fact]
    public void InvalidPatchDeltaException_HasMessage()
    {
        var exception = new InvalidPatchDeltaException();

        Assert.Contains("PATCH delta", exception.Message);
    }
}

