using System.Reflection;
using KvStore.Core.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace KvStore.Core.Application.Queries;

public sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = serviceProvider.GetRequiredService(handlerType);
        
        var method = handlerType.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        {
            throw new InvalidOperationException($"Handler type {handlerType.Name} does not implement HandleAsync method.");
        }
        
        var result = method.Invoke(handler, [query, cancellationToken]);
        return (Task<TResult>)result!;
    }
}

