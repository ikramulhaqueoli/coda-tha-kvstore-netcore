using System.Text.Json.Nodes;
using KvStore.Core.Application.Commands.PatchKeyValue;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using KvStore.Infrastructure.Concurrency;
using KvStore.Infrastructure.Persistence;
using KvStore.UnitTest.TestHelpers;

namespace KvStore.UnitTest.Core.Application.Commands;

public sealed class PatchKeyValueCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesNewAggregate_WhenKeyDoesNotExist()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PatchKeyValueCommandHandler(repository, lockProvider);
        var command = new PatchKeyValueCommand("new-key", JsonValue.Create(42), null);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("new-key", result.Key);
        Assert.Equal(42, result.Value?.GetValue<int>());
        Assert.Equal(1, result.Version);

        var stored = await repository.GetAsync("new-key");
        Assert.NotNull(stored);
        Assert.Equal(42, stored!.Value?.GetValue<int>());
    }

    [Fact]
    public async Task HandleAsync_MergesWithExistingAggregate_WhenKeyExists()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PatchKeyValueCommandHandler(repository, lockProvider);

        var existing = KeyValueAggregate.Create("existing", JsonNode.Parse("""{"count":10,"name":"test"}"""));
        await repository.UpsertAsync(existing);

        var delta = JsonNode.Parse("""{"count":20}""");
        var command = new PatchKeyValueCommand("existing", delta, null);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(20, result.Value?["count"]?.GetValue<int>());
        Assert.Equal("test", result.Value?["name"]?.GetValue<string>());
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task HandleAsync_WithExpectedVersion_MergesWhenVersionMatches()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PatchKeyValueCommandHandler(repository, lockProvider);

        var existing = KeyValueAggregate.Create("key", JsonNode.Parse("""{"count":10}"""));
        await repository.UpsertAsync(existing);

        var delta = JsonNode.Parse("""{"count":20}""");
        var command = new PatchKeyValueCommand("key", delta, 1);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(20, result.Value?["count"]?.GetValue<int>());
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task HandleAsync_ThrowsVersionMismatch_WhenExpectedVersionDoesNotMatch()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PatchKeyValueCommandHandler(repository, lockProvider);

        var existing = KeyValueAggregate.Create("key", JsonValue.Create(10));
        await repository.UpsertAsync(existing);

        var delta = JsonNode.Parse("""{"count":20}""");
        var command = new PatchKeyValueCommand("key", delta, 5);

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
        var handler = new PatchKeyValueCommandHandler(repository, lockProvider);

        var delta = JsonNode.Parse("""{"count":20}""");
        var command = new PatchKeyValueCommand("new-key", delta, 1);

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
        var handler = new PatchKeyValueCommandHandler(repository, lockProvider);

        var delta = JsonNode.Parse("""{"count":20}""");
        var command = new PatchKeyValueCommand("new-key", delta, 0);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(20, result.Value?["count"]?.GetValue<int>());
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task HandleAsync_WithNullDelta_StoresNull()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PatchKeyValueCommandHandler(repository, lockProvider);

        var command = new PatchKeyValueCommand("key", null, null);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Null(result.Value);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task HandleAsync_MergesNestedObjects()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PatchKeyValueCommandHandler(repository, lockProvider);

        var existing = KeyValueAggregate.Create("key", JsonNode.Parse("""{"app":{"name":"test","version":1}}"""));
        await repository.UpsertAsync(existing);

        var delta = JsonNode.Parse("""{"app":{"version":2}}""");
        var command = new PatchKeyValueCommand("key", delta, null);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Merge replaces nested objects at the top level, not deep merging
        var appObj = result.Value?["app"] as JsonObject;
        Assert.NotNull(appObj);
        // The entire "app" object is replaced, so "name" is lost
        Assert.Equal(2, appObj["version"]?.GetValue<int>());
    }

    [Fact]
    public async Task HandleAsync_ReplacesNonObjectValue_WhenMerging()
    {
        var repository = new InMemoryKeyValueRepository();
        var lockProvider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var handler = new PatchKeyValueCommandHandler(repository, lockProvider);

        var existing = KeyValueAggregate.Create("key", JsonValue.Create(10));
        await repository.UpsertAsync(existing);

        var delta = JsonNode.Parse("""{"count":20}""");
        var command = new PatchKeyValueCommand("key", delta, null);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(20, result.Value?["count"]?.GetValue<int>());
    }
}

