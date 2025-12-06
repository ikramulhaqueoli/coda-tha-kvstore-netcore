using System.Text.Json.Nodes;
using KvStore.Core.Application.Queries.ListKeys;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Repositories;
using KvStore.Infrastructure.Persistence;

namespace KvStore.UnitTest.Core.Application.Queries;

public sealed class ListKeysQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsEmptyCollection_WhenNoKeysExist()
    {
        var repository = new InMemoryKeyValueRepository();
        var handler = new ListKeysQueryHandler(repository);

        var query = new ListKeysQuery();
        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAllKeys()
    {
        var repository = new InMemoryKeyValueRepository();
        var handler = new ListKeysQueryHandler(repository);

        await repository.UpsertAsync(KeyValueAggregate.Create("key1", JsonValue.Create(1)));
        await repository.UpsertAsync(KeyValueAggregate.Create("key2", JsonValue.Create(2)));
        await repository.UpsertAsync(KeyValueAggregate.Create("key3", JsonValue.Create(3)));

        var query = new ListKeysQuery();
        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Contains("key1", result);
        Assert.Contains("key2", result);
        Assert.Contains("key3", result);
    }

    [Fact]
    public async Task HandleAsync_ReturnsKeysInAnyOrder()
    {
        var repository = new InMemoryKeyValueRepository();
        var handler = new ListKeysQueryHandler(repository);

        await repository.UpsertAsync(KeyValueAggregate.Create("z-key", JsonValue.Create(1)));
        await repository.UpsertAsync(KeyValueAggregate.Create("a-key", JsonValue.Create(2)));

        var query = new ListKeysQuery();
        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains("z-key", result);
        Assert.Contains("a-key", result);
    }
}

