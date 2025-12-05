using System.Text.Json.Nodes;
using KvStore.Core.Application.Commands.PatchKeyValue;
using KvStore.Core.Application.Commands.PutKeyValue;
using KvStore.Core.Application.Queries.GetKeyValue;
using KvStore.Core.Domain.Exceptions;
using KvStore.Infrastructure.Concurrency;
using KvStore.Infrastructure.Persistence;

namespace KvStore.Tests.KeyValue;

public sealed class KeyValueCommandHandlerTests : IDisposable
{
    private readonly InMemoryKeyValueRepository _repository = new();
    private readonly PerKeySemaphoreLockProvider _lockProvider = new();
    private readonly PutKeyValueCommandHandler _putHandler;
    private readonly PatchKeyValueCommandHandler _patchHandler;
    private readonly GetKeyValueQueryHandler _getHandler;

    public KeyValueCommandHandlerTests()
    {
        _putHandler = new PutKeyValueCommandHandler(_repository, _lockProvider);
        _patchHandler = new PatchKeyValueCommandHandler(_repository, _lockProvider);
        _getHandler = new GetKeyValueQueryHandler(_repository, _lockProvider);
    }

    public void Dispose()
    {
        _lockProvider.Dispose();
    }

    [Fact]
    public async Task Patch_Merges_Shallow_Object()
    {
        var initial = JsonNode.Parse("""{"name":"Ari","points":10}""");
        await _putHandler.Handle(new PutKeyValueCommand("user1", initial, null), CancellationToken.None);

        var delta = JsonNode.Parse("""{"rank":"gold","points":15}""");
        var response = await _patchHandler.Handle(new PatchKeyValueCommand("user1", delta, null), CancellationToken.None);

        Assert.Equal(2, response.Version);
        Assert.Equal("gold", response.Value?["rank"]?.GetValue<string>());
        Assert.Equal(15, response.Value?["points"]?.GetValue<int>());
        Assert.Equal("Ari", response.Value?["name"]?.GetValue<string>());
    }

    [Fact]
    public async Task Patch_Replaces_When_Not_Object()
    {
        await _putHandler.Handle(new PutKeyValueCommand("user2", JsonValue.Create("text"), null), CancellationToken.None);
        var response = await _patchHandler.Handle(new PatchKeyValueCommand("user2", JsonValue.Create(42), null), CancellationToken.None);

        Assert.Equal(2, response.Version);
        Assert.Equal(42, response.Value?.GetValue<int>());
    }

    [Fact]
    public async Task Put_With_Mismatched_Version_Throws()
    {
        await _putHandler.Handle(new PutKeyValueCommand("user3", JsonValue.Create(1), null), CancellationToken.None);
        await Assert.ThrowsAsync<VersionMismatchException>(() =>
            _putHandler.Handle(new PutKeyValueCommand("user3", JsonValue.Create(2), 99), CancellationToken.None));
    }

    [Fact]
    public async Task Put_With_Expected_Version_Zero_Creates_New_Value()
    {
        var response = await _putHandler.Handle(new PutKeyValueCommand("user5", JsonValue.Create(10), 0), CancellationToken.None);
        Assert.Equal(1, response.Version);
        Assert.Equal(10, response.Value?.GetValue<int>());
    }

    [Fact]
    public async Task Patch_With_Expected_Version_Zero_Creates_New_Value()
    {
        var delta = JsonNode.Parse("""{"points":5}""");
        var response = await _patchHandler.Handle(new PatchKeyValueCommand("user6", delta, 0), CancellationToken.None);
        Assert.Equal(1, response.Version);
        Assert.Equal(5, response.Value?["points"]?.GetValue<int>());
    }

    [Fact]
    public async Task Get_Missing_Key_Throws()
    {
        await Assert.ThrowsAsync<KeyValueNotFoundException>(() =>
            _getHandler.Handle(new GetKeyValueQuery("missingKey"), CancellationToken.None));
    }

    [Fact]
    public async Task Concurrent_Clients_Increment_Without_Lost_Updates()
    {
        const string key = "counter1";
        const int clients = 3;
        const int incrementsPerClient = 100;

        await _putHandler.Handle(new PutKeyValueCommand(key, JsonValue.Create(0), null), CancellationToken.None);

        var tasks = Enumerable.Range(0, clients)
            .Select(_ => Task.Run(() => IncrementAsync(key, incrementsPerClient)));

        await Task.WhenAll(tasks);

        var snapshot = await _getHandler.Handle(new GetKeyValueQuery(key), CancellationToken.None);
        Assert.Equal(clients * incrementsPerClient, snapshot.Value?.GetValue<int>());
    }

    private async Task IncrementAsync(string key, int iterations)
    {
        for (var i = 0; i < iterations; i++)
        {
            var updated = false;
            while (!updated)
            {
                var snapshot = await _getHandler.Handle(new GetKeyValueQuery(key), CancellationToken.None);
                var nextValue = snapshot.Value!.GetValue<int>() + 1;
                var payload = JsonValue.Create(nextValue);

                try
                {
                    await _putHandler.Handle(
                        new PutKeyValueCommand(key, payload, snapshot.Version),
                        CancellationToken.None);
                    updated = true;
                }
                catch (VersionMismatchException)
                {
                    await Task.Delay(Random.Shared.Next(1, 3));
                }
            }
        }
    }
}

