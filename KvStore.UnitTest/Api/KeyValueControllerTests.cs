using System.Text.Json.Nodes;
using KvStore.Api.Controllers;
using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Commands.PatchKeyValue;
using KvStore.Core.Application.Commands.PutKeyValue;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Core.Application.Queries.ListKeys;
using Microsoft.AspNetCore.Mvc;

namespace KvStore.UnitTest.Api;

public sealed class KeyValueControllerTests
{
    [Fact]
    public async Task ListAsync_UsesQueryDispatcherAndReturnsOk()
    {
        var keys = new[] { "alpha", "beta" };
        var queryDispatcher = new TestQueryDispatcher();
        queryDispatcher.SetResult<IReadOnlyCollection<string>>(keys);

        var controller = new KeyValueController(queryDispatcher, new TestCommandDispatcher());

        var result = await controller.ListAsync(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(keys, okResult.Value);
        Assert.Single(queryDispatcher.DispatchedQueries);
        Assert.IsType<ListKeysQuery>(queryDispatcher.DispatchedQueries[0]);
    }

    [Fact]
    public async Task PutAsync_DispatchesCommandWithExpectedPayload()
    {
        var expectedResponse = new KeyValueResponse("counter", JsonValue.Create(2), 5);
        var commandDispatcher = new TestCommandDispatcher();
        commandDispatcher.SetResult(expectedResponse);

        var controller = new KeyValueController(new TestQueryDispatcher(), commandDispatcher);
        var body = JsonNode.Parse("""{"count":2}""");

        var result = await controller.PutAsync("counter", body, expectedVersion: 4, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expectedResponse, okResult.Value);

        var dispatchedCommand = Assert.IsType<PutKeyValueCommand>(commandDispatcher.DispatchedCommands.Single());
        Assert.Equal("counter", dispatchedCommand.Key);
        Assert.Equal(4, dispatchedCommand.ExpectedVersion);
        Assert.Equal(2, dispatchedCommand.Value?["count"]?.GetValue<int>());
    }

    [Fact]
    public async Task PatchAsync_DispatchesDeltaAndReturnsResponse()
    {
        var response = new KeyValueResponse("counter", JsonValue.Create(3), 6);
        var commandDispatcher = new TestCommandDispatcher();
        commandDispatcher.SetResult(response);

        var controller = new KeyValueController(new TestQueryDispatcher(), commandDispatcher);
        var delta = JsonNode.Parse("""{"count": {"$inc": 1}}""");

        var result = await controller.PatchAsync("counter", delta, expectedVersion: null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, okResult.Value);

        var dispatchedCommand = Assert.IsType<PatchKeyValueCommand>(commandDispatcher.DispatchedCommands.Single());
        Assert.Equal("counter", dispatchedCommand.Key);
        Assert.Null(dispatchedCommand.ExpectedVersion);
        Assert.Equal(1, dispatchedCommand.Delta?["count"]?["$inc"]?.GetValue<int>());
    }

    private sealed class TestQueryDispatcher : IQueryDispatcher
    {
        private readonly Dictionary<Type, object?> _results = new();

        public List<object> DispatchedQueries { get; } = new();

        public void SetResult<TResult>(TResult result)
        {
            _results[typeof(TResult)] = result;
        }

        public Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            DispatchedQueries.Add(query);
            if (!_results.TryGetValue(typeof(TResult), out var value))
            {
                throw new InvalidOperationException($"No result registered for {typeof(TResult).Name}.");
            }

            return Task.FromResult((TResult)value!);
        }
    }

    private sealed class TestCommandDispatcher : ICommandDispatcher
    {
        private readonly Dictionary<Type, object?> _results = new();

        public List<object> DispatchedCommands { get; } = new();

        public void SetResult<TResult>(TResult result)
        {
            _results[typeof(TResult)] = result;
        }

        public Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            DispatchedCommands.Add(command);
            if (!_results.TryGetValue(typeof(TResult), out var value))
            {
                throw new InvalidOperationException($"No result registered for {typeof(TResult).Name}.");
            }

            return Task.FromResult((TResult)value!);
        }
    }
}

