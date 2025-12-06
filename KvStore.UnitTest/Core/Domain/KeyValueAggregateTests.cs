using System.Text.Json.Nodes;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Validation;
using Xunit;

namespace KvStore.UnitTest.Core.Domain;

public sealed class KeyValueAggregateTests
{
    [Fact]
    public void Create_WithValidKey_ReturnsAggregateWithVersionOne()
    {
        var key = "test-key";
        var value = JsonValue.Create(42);

        var aggregate = KeyValueAggregate.Create(key, value);

        Assert.Equal(key, aggregate.Key);
        Assert.Equal(42, aggregate.Value!.GetValue<int>());
        Assert.Equal(1, aggregate.Version);
    }

    [Fact]
    public void Create_WithNullValue_CreatesAggregateWithNullValue()
    {
        var key = "test-key";

        var aggregate = KeyValueAggregate.Create(key, null);

        Assert.Equal(key, aggregate.Key);
        Assert.Null(aggregate.Value);
        Assert.Equal(1, aggregate.Version);
    }

    [Fact]
    public void Create_WithInvalidKey_ThrowsInvalidKeyException()
    {
        var invalidKey = "   ";

        Assert.Throws<InvalidKeyException>(() => KeyValueAggregate.Create(invalidKey, JsonValue.Create(1)));
    }

    [Fact]
    public void Replace_WithoutVersionGuard_ReplacesValueAndIncrementsVersion()
    {
        var aggregate = KeyValueAggregate.Create("key", JsonValue.Create(1));

        aggregate.Replace(JsonValue.Create(2), null);

        Assert.Equal(2, aggregate.Value!.GetValue<int>());
        Assert.Equal(2, aggregate.Version);
    }

    [Fact]
    public void Replace_WithMatchingVersion_ReplacesValueAndIncrementsVersion()
    {
        var aggregate = KeyValueAggregate.Create("key", JsonValue.Create(1));

        aggregate.Replace(JsonValue.Create(2), 1);

        Assert.Equal(2, aggregate.Value!.GetValue<int>());
        Assert.Equal(2, aggregate.Version);
    }

    [Fact]
    public void Replace_WithMismatchedVersion_ThrowsVersionMismatchException()
    {
        var aggregate = KeyValueAggregate.Create("key", JsonValue.Create(1));

        Assert.Throws<VersionMismatchException>(() => aggregate.Replace(JsonValue.Create(2), 5));
        Assert.Equal(1, aggregate.Value!.GetValue<int>());
        Assert.Equal(1, aggregate.Version);
    }

    [Fact]
    public void Merge_WithObjectDelta_MergesPropertiesShallowly()
    {
        var original = JsonNode.Parse("""{"name":"Ari","points":10}""");
        var aggregate = KeyValueAggregate.Create("key", original);
        var delta = JsonNode.Parse("""{"rank":"gold"}""");

        aggregate.Merge(delta, null);

        var result = aggregate.Value!.AsObject();
        Assert.Equal("Ari", result["name"]!.GetValue<string>());
        Assert.Equal(10, result["points"]!.GetValue<int>());
        Assert.Equal("gold", result["rank"]!.GetValue<string>());
        Assert.Equal(2, aggregate.Version);
    }

    [Fact]
    public void Merge_WithPrimitiveDelta_ReplacesEntireValue()
    {
        var original = JsonNode.Parse("""{"name":"Ari"}""");
        var aggregate = KeyValueAggregate.Create("key", original);

        aggregate.Merge(JsonValue.Create(123), null);

        Assert.Equal(123, aggregate.Value!.GetValue<int>());
        Assert.Equal(2, aggregate.Version);
    }

    [Fact]
    public void Merge_WithMismatchedVersion_ThrowsVersionMismatchException()
    {
        var aggregate = KeyValueAggregate.Create("key", JsonValue.Create(1));
        var delta = JsonValue.Create(2);

        Assert.Throws<VersionMismatchException>(() => aggregate.Merge(delta, 5));
        Assert.Equal(1, aggregate.Value!.GetValue<int>());
        Assert.Equal(1, aggregate.Version);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var original = KeyValueAggregate.Create("key", JsonValue.Create(42));
        original.Replace(JsonValue.Create(100), null);

        var clone = original.Clone();

        Assert.Equal(original.Key, clone.Key);
        Assert.Equal(original.Version, clone.Version);
        Assert.Equal(original.Value!.GetValue<int>(), clone.Value!.GetValue<int>());

        original.Replace(JsonValue.Create(200), 2);
        Assert.Equal(100, clone.Value!.GetValue<int>());
        Assert.Equal(2, clone.Version);
    }

    [Fact]
    public void ToSnapshot_ReturnsCorrectSnapshot()
    {
        var aggregate = KeyValueAggregate.Create("key", JsonValue.Create(42));
        aggregate.Replace(JsonValue.Create(100), null);

        var snapshot = aggregate.ToSnapshot();

        Assert.Equal("key", snapshot.Key);
        Assert.Equal(100, snapshot.Value!.GetValue<int>());
        Assert.Equal(2, snapshot.Version);
    }

    [Fact]
    public void Replace_WithNullValue_ReplacesWithNull()
    {
        var aggregate = KeyValueAggregate.Create("key", JsonValue.Create(42));

        aggregate.Replace(null, null);

        Assert.Null(aggregate.Value);
        Assert.Equal(2, aggregate.Version);
    }
}
