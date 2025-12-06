using System.Text.Json.Nodes;
using KvStore.Core.Application.Queries.GetKeyValue;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using KvStore.Infrastructure.Concurrency;
using KvStore.Infrastructure.Persistence;
using KvStore.UnitTest.TestHelpers;

namespace KvStore.UnitTest.Core.Application.Queries;

public sealed class GetKeyValueQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsAggregate_WhenKeyExists()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new GetKeyValueQueryHandler(repository, lockProvider);

        var aggregate = KeyValueAggregate.Create("test", JsonValue.Create(42));
        await repository.UpsertAsync(aggregate);

        var query = new GetKeyValueQuery("test");
        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.Equal("test", result.Key);
        Assert.Equal(42, result.Value?.GetValue<int>());
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task HandleAsync_ThrowsKeyValueNotFoundException_WhenKeyDoesNotExist()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new GetKeyValueQueryHandler(repository, lockProvider);

        var query = new GetKeyValueQuery("nonexistent");

        var exception = await Assert.ThrowsAsync<KeyValueNotFoundException>(
            () => handler.HandleAsync(query, CancellationToken.None));

        Assert.Equal("nonexistent", exception.Key);
    }

    [Fact]
    public async Task HandleAsync_ReturnsComplexJsonObject()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new GetKeyValueQueryHandler(repository, lockProvider);

        var json = JsonNode.Parse("""{"name":"test","count":42,"tags":["a","b"]}""");
        var aggregate = KeyValueAggregate.Create("complex", json);
        await repository.UpsertAsync(aggregate);

        var query = new GetKeyValueQuery("complex");
        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.Equal("test", result.Value?["name"]?.GetValue<string>());
        Assert.Equal(42, result.Value?["count"]?.GetValue<int>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsNullValue_WhenStored()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new GetKeyValueQueryHandler(repository, lockProvider);

        var aggregate = KeyValueAggregate.Create("null-key", null);
        await repository.UpsertAsync(aggregate);

        var query = new GetKeyValueQuery("null-key");
        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.Null(result.Value);
        Assert.Equal(1, result.Version);
    }
}

