using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Validation;

namespace KvStore.UnitTest.Core.Domain;

public sealed class KeyValidatorTests
{
    [Fact]
    public void EnsureValid_AcceptsAlphanumericKeys()
    {
        KeyValidator.EnsureValid("abc123");
        KeyValidator.EnsureValid("ABC123");
        KeyValidator.EnsureValid("key1");
    }

    [Fact]
    public void EnsureValid_AcceptsAllowedSpecialCharacters()
    {
        KeyValidator.EnsureValid("key:value");
        KeyValidator.EnsureValid("key-value");
        KeyValidator.EnsureValid("key_value");
        KeyValidator.EnsureValid("key=value");
        KeyValidator.EnsureValid("key.value");
        KeyValidator.EnsureValid("key*value");
        KeyValidator.EnsureValid("key,value");
        KeyValidator.EnsureValid("key@value");
        KeyValidator.EnsureValid("key#value");
    }

    [Fact]
    public void EnsureValid_Throws_WhenKeyIsNull()
    {
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid(null!));
    }

    [Fact]
    public void EnsureValid_Throws_WhenKeyIsEmpty()
    {
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid(""));
    }

    [Fact]
    public void EnsureValid_Throws_WhenKeyIsWhitespace()
    {
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid(" "));
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid("  "));
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid("\t"));
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid("\n"));
    }

    [Fact]
    public void EnsureValid_Throws_WhenKeyContainsInvalidCharacters()
    {
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid("key/value"));
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid("key value"));
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid("key$value"));
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid("key%value"));
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid("key&value"));
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid("key(value)"));
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid("key[value]"));
        Assert.Throws<InvalidKeyException>(() => KeyValidator.EnsureValid("key{value}"));
    }

    [Fact]
    public void EnsureValid_AcceptsComplexValidKeys()
    {
        KeyValidator.EnsureValid("user:123:profile");
        KeyValidator.EnsureValid("app.config.database");
        KeyValidator.EnsureValid("key-value_pair=test@domain.com#tag");
    }
}

