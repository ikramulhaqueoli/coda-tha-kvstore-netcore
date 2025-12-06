using System.Text.Json.Nodes;
using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Commands.PutKeyValue;
using KvStore.Core.Application.Commands.Responses;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using Moq;
using Xunit;

namespace KvStore.UnitTest.Core.Application;

public sealed class PutKeyValueCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_NewKey_CreatesAggregateWithVersionOne()
    {
        var key = "test-key";
        var value = JsonValue.Create(42);
        var command = new PutKeyValueCommand(key, value, null);

        var repository = new Mock<IKeyValueRepository>();
        repository.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KeyValueAggregate?)null);

        var lockProvider = new Mock<IKeyLockProvider>();
        lockProvider.Setup(l => l.ExecuteWithLockAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<KeyValueResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<KeyValueResponse>>, CancellationToken>(async (k, action, ct) => await action(ct));

        var handler = new PutKeyValueCommandHandler(repository.Object, lockProvider.Object);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(key, result.Key);
        Assert.Equal(42, result.Value!.GetValue<int>());
        Assert.Equal(1, result.Version);
        repository.Verify(r => r.UpsertAsync(It.Is<KeyValueAggregate>(a => a.Key == key && a.Version == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExistingKey_ReplacesValueAndIncrementsVersion()
    {
        var key = "test-key";
        var existing = KeyValueAggregate.Create(key, JsonValue.Create(1));
        var newValue = JsonValue.Create(2);
        var command = new PutKeyValueCommand(key, newValue, null);

        var repository = new Mock<IKeyValueRepository>();
        repository.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var lockProvider = new Mock<IKeyLockProvider>();
        lockProvider.Setup(l => l.ExecuteWithLockAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<KeyValueResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<KeyValueResponse>>, CancellationToken>(async (k, action, ct) => await action(ct));

        var handler = new PutKeyValueCommandHandler(repository.Object, lockProvider.Object);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(2, result.Value!.GetValue<int>());
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task HandleAsync_WithMatchingVersion_Succeeds()
    {
        var key = "test-key";
        var existing = KeyValueAggregate.Create(key, JsonValue.Create(1));
        var command = new PutKeyValueCommand(key, JsonValue.Create(2), 1);

        var repository = new Mock<IKeyValueRepository>();
        repository.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var lockProvider = new Mock<IKeyLockProvider>();
        lockProvider.Setup(l => l.ExecuteWithLockAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<KeyValueResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<KeyValueResponse>>, CancellationToken>(async (k, action, ct) => await action(ct));

        var handler = new PutKeyValueCommandHandler(repository.Object, lockProvider.Object);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(2, result.Value!.GetValue<int>());
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task HandleAsync_WithMismatchedVersion_ThrowsVersionMismatchException()
    {
        var key = "test-key";
        var existing = KeyValueAggregate.Create(key, JsonValue.Create(1));
        var command = new PutKeyValueCommand(key, JsonValue.Create(2), 5);

        var repository = new Mock<IKeyValueRepository>();
        repository.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var lockProvider = new Mock<IKeyLockProvider>();
        lockProvider.Setup(l => l.ExecuteWithLockAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<KeyValueResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<KeyValueResponse>>, CancellationToken>(async (k, action, ct) => await action(ct));

        var handler = new PutKeyValueCommandHandler(repository.Object, lockProvider.Object);

        await Assert.ThrowsAsync<VersionMismatchException>(() => handler.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NewKeyWithNonZeroExpectedVersion_ThrowsVersionMismatchException()
    {
        var key = "test-key";
        var command = new PutKeyValueCommand(key, JsonValue.Create(1), 1);

        var repository = new Mock<IKeyValueRepository>();
        repository.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KeyValueAggregate?)null);

        var lockProvider = new Mock<IKeyLockProvider>();
        lockProvider.Setup(l => l.ExecuteWithLockAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<KeyValueResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<KeyValueResponse>>, CancellationToken>(async (k, action, ct) => await action(ct));

        var handler = new PutKeyValueCommandHandler(repository.Object, lockProvider.Object);

        await Assert.ThrowsAsync<VersionMismatchException>(() => handler.HandleAsync(command, CancellationToken.None));
    }
}

