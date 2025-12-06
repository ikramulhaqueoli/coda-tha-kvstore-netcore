using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Commands.Responses;
using KvStore.Core.Application.Queries.GetKeyValue;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using Moq;
using Xunit;

namespace KvStore.UnitTest.Core.Application;

public sealed class GetKeyValueQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingKey_ReturnsKeyValueResponse()
    {
        var key = "test-key";
        var aggregate = KeyValueAggregate.Create(key, System.Text.Json.Nodes.JsonValue.Create(42));
        var query = new GetKeyValueQuery(key);

        var repository = new Mock<IKeyValueRepository>();
        repository.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregate);

        var lockProvider = new Mock<IKeyLockProvider>();
        lockProvider.Setup(l => l.ExecuteWithLockAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<KeyValueResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<KeyValueResponse>>, CancellationToken>(async (k, action, ct) => await action(ct));

        var handler = new GetKeyValueQueryHandler(repository.Object, lockProvider.Object);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.Equal(key, result.Key);
        Assert.Equal(42, result.Value!.GetValue<int>());
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task HandleAsync_NonExistentKey_ThrowsKeyValueNotFoundException()
    {
        var key = "non-existent-key";
        var query = new GetKeyValueQuery(key);

        var repository = new Mock<IKeyValueRepository>();
        repository.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KeyValueAggregate?)null);

        var lockProvider = new Mock<IKeyLockProvider>();
        lockProvider.Setup(l => l.ExecuteWithLockAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<KeyValueResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<KeyValueResponse>>, CancellationToken>(async (k, action, ct) => await action(ct));

        var handler = new GetKeyValueQueryHandler(repository.Object, lockProvider.Object);

        await Assert.ThrowsAsync<KeyValueNotFoundException>(() => handler.HandleAsync(query, CancellationToken.None));
    }
}

