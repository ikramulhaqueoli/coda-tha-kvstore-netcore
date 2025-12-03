using System.Text.Json.Nodes;
using KvStore.Core.Application.KeyValue.Commands.PatchKeyValue;
using KvStore.Core.Application.KeyValue.Commands.PutKeyValue;
using KvStore.Core.Domain.Exceptions;

namespace KvStore.Tests.KeyValue;

public sealed class KeyValueCommandValidatorTests
{
    private readonly PutKeyValueCommandValidator _putValidator = new();
    private readonly PatchKeyValueCommandValidator _patchValidator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("user:bad")]
    [InlineData("bad-key")]
    public void Put_Command_Invalid_Key_Fails(string key)
    {
        Assert.Throws<InvalidKeyException>(() =>
            _putValidator.Validate(new PutKeyValueCommand(key, JsonValue.Create(1), null)));
    }

    [Fact]
    public void Patch_Command_Null_Delta_Fails()
    {
        Assert.Throws<InvalidPatchDeltaException>(() =>
            _patchValidator.Validate(new PatchKeyValueCommand("user4", null, null)));
    }
}

