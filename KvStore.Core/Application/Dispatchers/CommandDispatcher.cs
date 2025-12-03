using KvStore.Core.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace KvStore.Core.Application.Dispatchers;

public sealed class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        dynamic handler = serviceProvider.GetRequiredService(handlerType);
        return handler.Handle((dynamic)command, cancellationToken);
    }
}

