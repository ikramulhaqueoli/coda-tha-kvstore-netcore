using System.Text.Json.Nodes;
using KvStore.Core.Domain.Entities;
using KvStore.Infrastructure.Persistence;

namespace KvStore.UnitTest.Infrastructure.Persistence;

public sealed class InMemoryKeyValueRepositoryTests
{
    [Fact]
    public async Task GetAsync_ReturnsNull_WhenKeyDoesNotExist()
    {
        var repository = new InMemoryKeyValueRepository();

        var result = await repository.GetAsync("nonexistent");

        Assert.Null(result);
    }

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

    [Fact]
    public async Task UpsertAsync_StoresNewAggregate()
    {
        var repository = new InMemoryKeyValueRepository();
        var aggregate = KeyValueAggregate.Create("test", JsonValue.Create(42));

        await repository.UpsertAsync(aggregate);

        var retrieved = await repository.GetAsync("test");
        Assert.NotNull(retrieved);
        Assert.Equal(42, retrieved!.Value?.GetValue<int>());
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExistingAggregate()
    {
        var repository = new InMemoryKeyValueRepository();
        var original = KeyValueAggregate.Create("test", JsonValue.Create(10));
        await repository.UpsertAsync(original);

        original.Replace(JsonValue.Create(20), original.Version);
        await repository.UpsertAsync(original);

        var retrieved = await repository.GetAsync("test");
        Assert.Equal(20, retrieved!.Value?.GetValue<int>());
    }

    [Fact]
    public async Task ListKeysAsync_ReturnsEmptyCollection_WhenNoKeys()
    {
        var repository = new InMemoryKeyValueRepository();

        var keys = await repository.ListKeysAsync();

        Assert.Empty(keys);
    }

    [Fact]
    public async Task ListKeysAsync_ReturnsAllKeys()
    {
        var repository = new InMemoryKeyValueRepository();
        await repository.UpsertAsync(KeyValueAggregate.Create("key1", JsonValue.Create(1)));
        await repository.UpsertAsync(KeyValueAggregate.Create("key2", JsonValue.Create(2)));
        await repository.UpsertAsync(KeyValueAggregate.Create("key3", JsonValue.Create(3)));

        var keys = await repository.ListKeysAsync();

        Assert.Equal(3, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
        Assert.Contains("key3", keys);
    }

    [Fact]
    public async Task GetAsync_IsCaseSensitive()
    {
        var repository = new InMemoryKeyValueRepository();
        await repository.UpsertAsync(KeyValueAggregate.Create("Test", JsonValue.Create(1)));

        var result = await repository.GetAsync("test");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertAsync_OverwritesExistingKey()
    {
        var repository = new InMemoryKeyValueRepository();
        var first = KeyValueAggregate.Create("key", JsonValue.Create(1));
        await repository.UpsertAsync(first);

        var second = KeyValueAggregate.Create("key", JsonValue.Create(2));
        await repository.UpsertAsync(second);

        var retrieved = await repository.GetAsync("key");
        Assert.Equal(2, retrieved!.Value?.GetValue<int>());
    }

    [Fact]
    public async Task GetAsync_Throws_WhenCancellationRequested()
    {
        var repository = new InMemoryKeyValueRepository();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => repository.GetAsync("key", cts.Token));
    }

    [Fact]
    public async Task UpsertAsync_Throws_WhenCancellationRequested()
    {
        var repository = new InMemoryKeyValueRepository();
        var aggregate = KeyValueAggregate.Create("key", JsonValue.Create(1));
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => repository.UpsertAsync(aggregate, cts.Token));
    }

    [Fact]
    public async Task ListKeysAsync_Throws_WhenCancellationRequested()
    {
        var repository = new InMemoryKeyValueRepository();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => repository.ListKeysAsync(cts.Token));
    }
}


