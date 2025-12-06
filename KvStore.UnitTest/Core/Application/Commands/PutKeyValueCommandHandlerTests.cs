using System.Text.Json.Nodes;
using KvStore.Core.Application.Commands.PutKeyValue;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using KvStore.Infrastructure.Concurrency;
using KvStore.Infrastructure.Persistence;
using KvStore.UnitTest.TestHelpers;

namespace KvStore.UnitTest.Core.Application.Commands;

public sealed class PutKeyValueCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesNewAggregate_WhenKeyDoesNotExist()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PutKeyValueCommandHandler(repository, lockProvider);
        var command = new PutKeyValueCommand("new-key", JsonValue.Create(42), null);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("new-key", result.Key);
        Assert.Equal(42, result.Value?.GetValue<int>());
        Assert.Equal(1, result.Version);

        var stored = await repository.GetAsync("new-key");
        Assert.NotNull(stored);
        Assert.Equal(42, stored!.Value?.GetValue<int>());
    }

    [Fact]
    public async Task HandleAsync_UpdatesExistingAggregate_WhenKeyExists()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PutKeyValueCommandHandler(repository, lockProvider);

        var existing = KeyValueAggregate.Create("existing", JsonValue.Create(10));
        await repository.UpsertAsync(existing);

        var command = new PutKeyValueCommand("existing", JsonValue.Create(20), null);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(20, result.Value?.GetValue<int>());
        Assert.Equal(2, result.Version);

        var stored = await repository.GetAsync("existing");
        Assert.Equal(20, stored!.Value?.GetValue<int>());
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task HandleAsync_WithExpectedVersion_UpdatesWhenVersionMatches()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PutKeyValueCommandHandler(repository, lockProvider);

        var existing = KeyValueAggregate.Create("key", JsonValue.Create(10));
        await repository.UpsertAsync(existing);

        var command = new PutKeyValueCommand("key", JsonValue.Create(20), 1);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(20, result.Value?.GetValue<int>());
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task HandleAsync_ThrowsVersionMismatch_WhenExpectedVersionDoesNotMatch()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PutKeyValueCommandHandler(repository, lockProvider);

        var existing = KeyValueAggregate.Create("key", JsonValue.Create(10));
        await repository.UpsertAsync(existing);

        var command = new PutKeyValueCommand("key", JsonValue.Create(20), 5);

        var exception = await Assert.ThrowsAsync<VersionMismatchException>(
            () => handler.HandleAsync(command, CancellationToken.None));

        Assert.Equal("key", exception.Key);
        Assert.Equal(5, exception.ExpectedVersion);
        Assert.Equal(1, exception.CurrentVersion);
    }

    [Fact]
    public async Task HandleAsync_ThrowsVersionMismatch_WhenCreatingWithNonZeroExpectedVersion()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PutKeyValueCommandHandler(repository, lockProvider);

        var command = new PutKeyValueCommand("new-key", JsonValue.Create(42), 1);

        var exception = await Assert.ThrowsAsync<VersionMismatchException>(
            () => handler.HandleAsync(command, CancellationToken.None));

        Assert.Equal("new-key", exception.Key);
        Assert.Equal(1, exception.ExpectedVersion);
        Assert.Equal(0, exception.CurrentVersion);
    }

    [Fact]
    public async Task HandleAsync_AllowsCreatingWithZeroExpectedVersion()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PutKeyValueCommandHandler(repository, lockProvider);

        var command = new PutKeyValueCommand("new-key", JsonValue.Create(42), 0);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(42, result.Value?.GetValue<int>());
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task HandleAsync_WithNullValue_StoresNull()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PutKeyValueCommandHandler(repository, lockProvider);

        var command = new PutKeyValueCommand("key", null, null);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Null(result.Value);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task HandleAsync_WithComplexJsonObject_StoresCorrectly()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PutKeyValueCommandHandler(repository, lockProvider);

        var json = JsonNode.Parse("""{"name":"test","count":42,"tags":["a","b"]}""");
        var command = new PutKeyValueCommand("complex", json, null);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("test", result.Value?["name"]?.GetValue<string>());
        Assert.Equal(42, result.Value?["count"]?.GetValue<int>());
    }
}

