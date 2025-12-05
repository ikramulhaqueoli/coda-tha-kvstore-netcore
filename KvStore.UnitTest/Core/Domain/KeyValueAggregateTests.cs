using System.Text.Json.Nodes;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;

namespace KvStore.UnitTest.Core.Domain;

public sealed class KeyValueAggregateTests
{
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
    public void Replace_Throws_WhenExpectedVersionDoesNotMatch()
    {
        var aggregate = KeyValueAggregate.Create("counter", JsonValue.Create(1));

        var exception = Assert.Throws<VersionMismatchException>(() =>
            aggregate.Replace(JsonValue.Create(2), expectedVersion: 5));

        Assert.Equal("counter", exception.Key);
        Assert.Equal(5, exception.ExpectedVersion);
        Assert.Equal(1, exception.CurrentVersion);
    }
}


