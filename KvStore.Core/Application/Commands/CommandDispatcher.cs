using System.Reflection;
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
        var handler = serviceProvider.GetRequiredService(handlerType);
        
        var method = handlerType.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        {
            throw new InvalidOperationException($"Handler type {handlerType.Name} does not implement HandleAsync method.");
        }
        
        var result = method.Invoke(handler, new object[] { command, cancellationToken });
        return (Task<TResult>)result!;
    }

    private void InvokeValidator(Type commandType, object command)
    {
        var validatorType = typeof(ICommandValidator<>).MakeGenericType(commandType);
        var validator = serviceProvider.GetService(validatorType);
        if (validator is null)
        {
            return;
        }

        var validateMethod = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
        if (validateMethod != null)
        {
            validateMethod.Invoke(validator, new object[] { command });
        }
    }
}

