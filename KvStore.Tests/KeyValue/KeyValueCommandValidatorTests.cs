using System.Text.Json.Nodes;
using KvStore.Core.Application.Commands.PatchKeyValue;
using KvStore.Core.Application.Commands.PutKeyValue;
using KvStore.Core.Domain.Exceptions;

namespace KvStore.Tests.KeyValue;

public sealed class KeyValueCommandValidatorTests
{
    private readonly PutKeyValueCommandValidator _putValidator = new();
    private readonly PatchKeyValueCommandValidator _patchValidator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad key")]
    [InlineData("bad/key")]
    public void Put_Command_Invalid_Key_Fails(string key)
    {
        Assert.Throws<InvalidKeyException>(() =>
            _putValidator.Validate(new PutKeyValueCommand(key, JsonValue.Create(1), null)));
    }

    [Theory]
    [InlineData("user:good")]
    [InlineData("user-good")]
    [InlineData("user_good")]
    [InlineData("user=good")]
    public void Put_Command_Allows_Configured_Symbols(string key)
    {
        var exception = Record.Exception(() =>
            _putValidator.Validate(new PutKeyValueCommand(key, JsonValue.Create(1), null)));
        Assert.Null(exception);
    }

    [Fact]
    public void Patch_Command_Null_Delta_Fails()
    {
        Assert.Throws<InvalidPatchDeltaException>(() =>
            _patchValidator.Validate(new PatchKeyValueCommand("user4", null, null)));
    }
}

