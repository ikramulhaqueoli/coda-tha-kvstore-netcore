using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Queries;
using KvStore.Core.Application.Queries.GetKeyValue;
using KvStore.Core.Application.KeyValue.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace KvStore.UnitTest.Core.Application.Queries;

public sealed class QueryDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ResolvesAndInvokesHandler()
    {
        var services = new ServiceCollection();
        var handler = new TestQueryHandler();
        services.AddSingleton<IQueryHandler<GetKeyValueQuery, KeyValueResponse>>(handler);
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new QueryDispatcher(serviceProvider);
        var query = new GetKeyValueQuery("test");

        var result = await dispatcher.DispatchAsync(query);

        Assert.True(handler.Handled);
        Assert.Equal("test", handler.Query?.Key);
    }

    [Fact]
    public async Task DispatchAsync_Throws_WhenHandlerNotRegistered()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new QueryDispatcher(serviceProvider);
        var query = new GetKeyValueQuery("test");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(query));
    }

    [Fact]
    public async Task DispatchAsync_Throws_WhenQueryIsNull()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new QueryDispatcher(serviceProvider);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => dispatcher.DispatchAsync<KeyValueResponse>(null!));
    }

    private sealed class TestQueryHandler : IQueryHandler<GetKeyValueQuery, KeyValueResponse>
    {
        public bool Handled { get; private set; }
        public GetKeyValueQuery? Query { get; private set; }

        public Task<KeyValueResponse> HandleAsync(GetKeyValueQuery query, CancellationToken cancellationToken = default)
        {
            Handled = true;
            Query = query;
            return Task.FromResult(new KeyValueResponse(query.Key, null, 1));
        }
    }
}

