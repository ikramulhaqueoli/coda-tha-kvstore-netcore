using System.Text.Json.Nodes;
using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Commands.PatchKeyValue;
using KvStore.Core.Application.Commands.Responses;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using Moq;
using Xunit;

namespace KvStore.UnitTest.Core.Application;

public sealed class PatchKeyValueCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_NewKey_CreatesAggregateWithDelta()
    {
        var key = "test-key";
        var delta = JsonNode.Parse("""{"value":42}""");
        var command = new PatchKeyValueCommand(key, delta, null);

        var repository = new Mock<IKeyValueRepository>();
        repository.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KeyValueAggregate?)null);

        var lockProvider = new Mock<IKeyLockProvider>();
        lockProvider.Setup(l => l.ExecuteWithLockAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<KeyValueResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<KeyValueResponse>>, CancellationToken>(async (k, action, ct) => await action(ct));

        var handler = new PatchKeyValueCommandHandler(repository.Object, lockProvider.Object);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(key, result.Key);
        Assert.Equal(42, result.Value!["value"]!.GetValue<int>());
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task HandleAsync_ExistingKeyWithObjectDelta_MergesProperties()
    {
        var key = "test-key";
        var existing = KeyValueAggregate.Create(key, JsonNode.Parse("""{"name":"Ari","points":10}"""));
        var delta = JsonNode.Parse("""{"rank":"gold"}""");
        var command = new PatchKeyValueCommand(key, delta, null);

        var repository = new Mock<IKeyValueRepository>();
        repository.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var lockProvider = new Mock<IKeyLockProvider>();
        lockProvider.Setup(l => l.ExecuteWithLockAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<KeyValueResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<KeyValueResponse>>, CancellationToken>(async (k, action, ct) => await action(ct));

        var handler = new PatchKeyValueCommandHandler(repository.Object, lockProvider.Object);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        var resultObj = result.Value!.AsObject();
        Assert.Equal("Ari", resultObj["name"]!.GetValue<string>());
        Assert.Equal(10, resultObj["points"]!.GetValue<int>());
        Assert.Equal("gold", resultObj["rank"]!.GetValue<string>());
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task HandleAsync_WithPrimitiveDelta_ReplacesValue()
    {
        var key = "test-key";
        var existing = KeyValueAggregate.Create(key, JsonNode.Parse("""{"name":"Ari"}"""));
        var command = new PatchKeyValueCommand(key, JsonValue.Create(123), null);

        var repository = new Mock<IKeyValueRepository>();
        repository.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var lockProvider = new Mock<IKeyLockProvider>();
        lockProvider.Setup(l => l.ExecuteWithLockAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<KeyValueResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<KeyValueResponse>>, CancellationToken>(async (k, action, ct) => await action(ct));

        var handler = new PatchKeyValueCommandHandler(repository.Object, lockProvider.Object);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(123, result.Value!.GetValue<int>());
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task HandleAsync_WithMismatchedVersion_ThrowsVersionMismatchException()
    {
        var key = "test-key";
        var existing = KeyValueAggregate.Create(key, JsonValue.Create(1));
        var command = new PatchKeyValueCommand(key, JsonValue.Create(2), 5);

        var repository = new Mock<IKeyValueRepository>();
        repository.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var lockProvider = new Mock<IKeyLockProvider>();
        lockProvider.Setup(l => l.ExecuteWithLockAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<KeyValueResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<KeyValueResponse>>, CancellationToken>(async (k, action, ct) => await action(ct));

        var handler = new PatchKeyValueCommandHandler(repository.Object, lockProvider.Object);

        await Assert.ThrowsAsync<VersionMismatchException>(() => handler.HandleAsync(command, CancellationToken.None));
    }
}

