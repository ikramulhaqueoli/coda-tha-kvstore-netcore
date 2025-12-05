using KvStore.Core.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace KvStore.Core.Application.Commands;

public sealed class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        InvokeValidator(commandType, command);

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        dynamic handler = serviceProvider.GetRequiredService(handlerType);
        return handler.Handle((dynamic)command, cancellationToken);
    }

    private void InvokeValidator(Type commandType, object command)
    {
        var validatorType = typeof(ICommandValidator<>).MakeGenericType(commandType);
        var validator = serviceProvider.GetService(validatorType);
        if (validator is null)
        {
            return;
        }

        ((dynamic)validator).Validate((dynamic)command);
    }
}

