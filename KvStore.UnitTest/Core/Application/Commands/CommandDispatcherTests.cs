using System.Text.Json.Nodes;
using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Commands;
using KvStore.Core.Application.Commands.PutKeyValue;
using KvStore.Core.Application.KeyValue.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace KvStore.UnitTest.Core.Application.Commands;

public sealed class CommandDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ResolvesAndInvokesHandler()
    {
        var services = new ServiceCollection();
        var handler = new TestCommandHandler();
        services.AddSingleton<ICommandHandler<PutKeyValueCommand, KeyValueResponse>>(handler);
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new CommandDispatcher(serviceProvider);
        var command = new PutKeyValueCommand("test", JsonValue.Create(42), null);

        var result = await dispatcher.DispatchAsync(command);

        Assert.True(handler.Handled);
        Assert.Equal("test", handler.Command?.Key);
    }

    [Fact]
    public async Task DispatchAsync_Throws_WhenHandlerNotRegistered()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new CommandDispatcher(serviceProvider);
        var command = new PutKeyValueCommand("test", JsonValue.Create(42), null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(command));
    }

    [Fact]
    public async Task DispatchAsync_Throws_WhenCommandIsNull()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new CommandDispatcher(serviceProvider);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => dispatcher.DispatchAsync<KeyValueResponse>(null!));
    }

    [Fact]
    public async Task DispatchAsync_InvokesValidator_WhenRegistered()
    {
        var services = new ServiceCollection();
        var handler = new TestCommandHandler();
        var validator = new TestCommandValidator();
        services.AddSingleton<ICommandHandler<PutKeyValueCommand, KeyValueResponse>>(handler);
        services.AddSingleton<ICommandValidator<PutKeyValueCommand>>(validator);
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new CommandDispatcher(serviceProvider);
        var command = new PutKeyValueCommand("test", JsonValue.Create(42), null);

        await dispatcher.DispatchAsync(command);

        Assert.True(validator.Validated);
    }

    [Fact]
    public async Task DispatchAsync_SkipsValidator_WhenNotRegistered()
    {
        var services = new ServiceCollection();
        var handler = new TestCommandHandler();
        services.AddSingleton<ICommandHandler<PutKeyValueCommand, KeyValueResponse>>(handler);
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new CommandDispatcher(serviceProvider);
        var command = new PutKeyValueCommand("test", JsonValue.Create(42), null);

        await dispatcher.DispatchAsync(command);

        Assert.True(handler.Handled);
    }

    private sealed class TestCommandHandler : ICommandHandler<PutKeyValueCommand, KeyValueResponse>
    {
        public bool Handled { get; private set; }
        public PutKeyValueCommand? Command { get; private set; }

        public Task<KeyValueResponse> HandleAsync(PutKeyValueCommand command, CancellationToken cancellationToken = default)
        {
            Handled = true;
            Command = command;
            return Task.FromResult(new KeyValueResponse(command.Key, command.Value, 1));
        }
    }

    private sealed class TestCommandValidator : ICommandValidator<PutKeyValueCommand>
    {
        public bool Validated { get; private set; }

        public void Validate(PutKeyValueCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            Validated = true;
        }
    }
}

