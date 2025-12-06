using System.Text.Json.Nodes;
using KvStore.Core.Domain.Entities;
using KvStore.Infrastructure.Persistence;
using Xunit;

namespace KvStore.UnitTest.Infrastructure.Persistence;

public sealed class InMemoryKeyValueRepositoryTests
{
    [Fact]
    public async Task GetAsync_NonExistentKey_ReturnsNull()
    {
        var repository = new InMemoryKeyValueRepository();

        var result = await repository.GetAsync("non-existent", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertAsync_ThenGetAsync_ReturnsClonedAggregate()
    {
        var repository = new InMemoryKeyValueRepository();
        var aggregate = KeyValueAggregate.Create("test-key", JsonValue.Create(42));

        await repository.UpsertAsync(aggregate, CancellationToken.None);
        var retrieved = await repository.GetAsync("test-key", CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal("test-key", retrieved!.Key);
        Assert.Equal(42, retrieved.Value!.GetValue<int>());
        Assert.Equal(1, retrieved.Version);

        aggregate.Replace(JsonValue.Create(100), null);
        Assert.Equal(42, retrieved.Value!.GetValue<int>());
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExistingKey()
    {
        var repository = new InMemoryKeyValueRepository();
        var original = KeyValueAggregate.Create("test-key", JsonValue.Create(1));
        await repository.UpsertAsync(original, CancellationToken.None);

        original.Replace(JsonValue.Create(2), null);
        await repository.UpsertAsync(original, CancellationToken.None);

        var retrieved = await repository.GetAsync("test-key", CancellationToken.None);
        Assert.Equal(2, retrieved!.Value!.GetValue<int>());
        Assert.Equal(2, retrieved.Version);
    }

    [Fact]
    public async Task ListKeysAsync_ReturnsAllKeys()
    {
        var repository = new InMemoryKeyValueRepository();
        await repository.UpsertAsync(KeyValueAggregate.Create("key1", JsonValue.Create(1)), CancellationToken.None);
        await repository.UpsertAsync(KeyValueAggregate.Create("key2", JsonValue.Create(2)), CancellationToken.None);
        await repository.UpsertAsync(KeyValueAggregate.Create("key3", JsonValue.Create(3)), CancellationToken.None);

        var keys = await repository.ListKeysAsync(CancellationToken.None);

        Assert.Equal(3, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
        Assert.Contains("key3", keys);
    }

    [Fact]
    public async Task ListKeysAsync_EmptyRepository_ReturnsEmptyCollection()
    {
        var repository = new InMemoryKeyValueRepository();

        var keys = await repository.ListKeysAsync(CancellationToken.None);

        Assert.Empty(keys);
    }
}
