using System.Text.Json.Nodes;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;

namespace KvStore.UnitTest.Core.Domain;

public sealed class KeyValueAggregateTests
{
    [Fact]
    public void Create_InitializesWithVersionOne()
    {
        var aggregate = KeyValueAggregate.Create("test", JsonValue.Create(42));

        Assert.Equal("test", aggregate.Key);
        Assert.Equal(42, aggregate.Value?.GetValue<int>());
        Assert.Equal(1, aggregate.Version);
    }

    [Fact]
    public void Create_WithNullValue_InitializesCorrectly()
    {
        var aggregate = KeyValueAggregate.Create("test", null);

        Assert.Equal("test", aggregate.Key);
        Assert.Null(aggregate.Value);
        Assert.Equal(1, aggregate.Version);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var original = KeyValueAggregate.Create("test", JsonNode.Parse("""{"count":1}"""));
        var clone = original.Clone();

        clone.Replace(JsonNode.Parse("""{"count":2}"""), clone.Version);

        Assert.Equal(1, original.Value?["count"]?.GetValue<int>());
        Assert.Equal(2, clone.Value?["count"]?.GetValue<int>());
        Assert.Equal(1, original.Version);
        Assert.Equal(2, clone.Version);
    }

    [Fact]
    public void Replace_UpdatesValueAndIncrementsVersion()
    {
        var aggregate = KeyValueAggregate.Create("counter", JsonValue.Create(1));
        var newValue = JsonValue.Create(2);

        aggregate.Replace(newValue, expectedVersion: null);

        Assert.Equal(2, aggregate.Value?.GetValue<int>());
        Assert.Equal(2, aggregate.Version);
    }

    [Fact]
    public void Replace_WithExpectedVersion_UpdatesWhenVersionMatches()
    {
        var aggregate = KeyValueAggregate.Create("counter", JsonValue.Create(1));

        aggregate.Replace(JsonValue.Create(2), expectedVersion: 1);

        Assert.Equal(2, aggregate.Value?.GetValue<int>());
        Assert.Equal(2, aggregate.Version);
    }

    [Fact]
    public void Replace_Throws_WhenExpectedVersionDoesNotMatch()
    {
        var aggregate = KeyValueAggregate.Create("counter", JsonValue.Create(1));

        var exception = Assert.Throws<VersionMismatchException>(() =>
            aggregate.Replace(JsonValue.Create(2), expectedVersion: 5));

        Assert.Equal("counter", exception.Key);
        Assert.Equal(5, exception.ExpectedVersion);
        Assert.Equal(1, exception.CurrentVersion);
    }

    [Fact]
    public void Replace_WithNullValue_ReplacesWithNull()
    {
        var aggregate = KeyValueAggregate.Create("test", JsonValue.Create(42));

        aggregate.Replace(null, expectedVersion: null);

        Assert.Null(aggregate.Value);
        Assert.Equal(2, aggregate.Version);
    }

    [Fact]
    public void Merge_MergesJsonObjectsAndPreservesExistingProperties()
    {
        var aggregate = KeyValueAggregate.Create(
            "settings",
            JsonNode.Parse("""{"count":1,"tags":{"env":"prod"}}"""));

        var delta = JsonNode.Parse("""{"count":2,"owner":"api"}""");

        aggregate.Merge(delta, expectedVersion: 1);

        var snapshot = aggregate.ToSnapshot();
        Assert.Equal(2, snapshot.Version);
        Assert.Equal(2, snapshot.Value?["count"]?.GetValue<int>());
        Assert.Equal("prod", snapshot.Value?["tags"]?["env"]?.GetValue<string>());
        Assert.Equal("api", snapshot.Value?["owner"]?.GetValue<string>());
    }

    [Fact]
    public void Merge_ReplacesValue_WhenExistingIsNotJsonObject()
    {
        var aggregate = KeyValueAggregate.Create("counter", JsonValue.Create(1));
        var delta = JsonNode.Parse("""{"count":2}""");

        aggregate.Merge(delta, expectedVersion: null);

        var snapshot = aggregate.ToSnapshot();
        Assert.Equal(2, snapshot.Version);
        Assert.Equal(2, snapshot.Value?["count"]?.GetValue<int>());
    }

    [Fact]
    public void Merge_Throws_WhenExpectedVersionDoesNotMatch()
    {
        var aggregate = KeyValueAggregate.Create("counter", JsonValue.Create(1));

        var exception = Assert.Throws<VersionMismatchException>(() =>
            aggregate.Merge(JsonValue.Create(2), expectedVersion: 5));

        Assert.Equal("counter", exception.Key);
        Assert.Equal(5, exception.ExpectedVersion);
        Assert.Equal(1, exception.CurrentVersion);
    }

    [Fact]
    public void Merge_WithNullDelta_ReplacesWithNull()
    {
        var aggregate = KeyValueAggregate.Create("test", JsonValue.Create(42));

        aggregate.Merge(null, expectedVersion: null);

        Assert.Null(aggregate.Value);
        Assert.Equal(2, aggregate.Version);
    }

    [Fact]
    public void Merge_WithNestedObjects_MergesCorrectly()
    {
        var aggregate = KeyValueAggregate.Create(
            "config",
            JsonNode.Parse("""{"app":{"name":"test","version":1}}"""));

        var delta = JsonNode.Parse("""{"app":{"version":2},"db":{"host":"localhost"}}""");

        aggregate.Merge(delta, expectedVersion: null);

        var snapshot = aggregate.ToSnapshot();
        // Merge replaces nested objects at the top level, not deep merging
        var appObj = snapshot.Value?["app"] as JsonObject;
        Assert.NotNull(appObj);
        // The entire "app" object is replaced, so "name" is lost
        Assert.Equal(2, appObj["version"]?.GetValue<int>());
        Assert.Equal("localhost", snapshot.Value?["db"]?["host"]?.GetValue<string>());
    }

    [Fact]
    public void ToSnapshot_CreatesIndependentSnapshot()
    {
        var aggregate = KeyValueAggregate.Create("test", JsonNode.Parse("""{"count":1}"""));
        var snapshot = aggregate.ToSnapshot();

        aggregate.Replace(JsonNode.Parse("""{"count":2}"""), aggregate.Version);

        Assert.Equal(1, snapshot.Value?["count"]?.GetValue<int>());
        Assert.Equal(1, snapshot.Version);
        Assert.Equal(2, aggregate.Version);
    }
}


