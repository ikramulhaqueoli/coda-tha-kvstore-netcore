using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Validation;
using Xunit;

namespace KvStore.UnitTest.Core.Domain;

public sealed class KeyValidatorTests
{
    [Theory]
    [InlineData("valid-key")]
    [InlineData("valid_key")]
    [InlineData("valid.key")]
    [InlineData("valid:key")]
    [InlineData("valid=key")]
    [InlineData("valid*key")]
    [InlineData("valid,key")]
    [InlineData("valid@key")]
    [InlineData("valid#key")]
    [InlineData("valid%key")]
    [InlineData("valid-key123")]
    [InlineData("ValidKey123")]
    [InlineData("a")]
    [InlineData("123")]
    public void EnsureValid_WithValidKey_DoesNotThrow(string key)
    {
        var exception = Record.Exception(() => KeyValidator.EnsureValid(key));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(null)]
    public void EnsureValid_WithWhitespaceOrNull_ThrowsInvalidKeyException(string? key)
    {
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid(key!));
    }

    [Theory]
    [InlineData("key with space")]
    [InlineData("key/with/slash")]
    [InlineData("key\\with\\backslash")]
    [InlineData("key|with|pipe")]
    [InlineData("key&with&ampersand")]
    [InlineData("key$with$dollar")]
    [InlineData("key!with!exclamation")]
    [InlineData("key?with?question")]
    [InlineData("key[with]brackets")]
    [InlineData("key{with}braces")]
    [InlineData("key(with)parentheses")]
    public void EnsureValid_WithInvalidCharacters_ThrowsInvalidKeyException(string key)
    {
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid(key));
    }
}
