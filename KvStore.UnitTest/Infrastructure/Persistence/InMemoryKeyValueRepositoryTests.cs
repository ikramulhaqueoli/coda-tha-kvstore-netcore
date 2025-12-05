using System.Text.Json.Nodes;
using KvStore.Core.Domain.Entities;
using KvStore.Infrastructure.Persistence;

namespace KvStore.UnitTest.Infrastructure.Persistence;

public sealed class InMemoryKeyValueRepositoryTests
{
    [Fact]
    public async Task UpsertAndGet_ReturnsClonedAggregate()
    {
        var repository = new InMemoryKeyValueRepository();
        var aggregate = KeyValueAggregate.Create("alpha", JsonNode.Parse("""{"count":1}"""));
        await repository.UpsertAsync(aggregate);

        var retrieved = await repository.GetAsync("alpha");
        Assert.NotNull(retrieved);

        retrieved!.Replace(JsonNode.Parse("""{"count":2}"""), retrieved.Version);

        var persisted = await repository.GetAsync("alpha");
        Assert.Equal(1, persisted!.Value?["count"]?.GetValue<int>());
    }
}


