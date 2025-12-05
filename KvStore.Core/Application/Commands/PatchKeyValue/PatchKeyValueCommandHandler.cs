using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace KvStore.Core.Application.Commands.PatchKeyValue;

public sealed class PatchKeyValueCommandHandler(
    IKeyValueRepository repository,
    IKeyLockProvider keyLockProvider,
    ILogger<PatchKeyValueCommandHandler> logger) : ICommandHandler<PatchKeyValueCommand, KeyValueResponse>
{
    public Task<KeyValueResponse> Handle(PatchKeyValueCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Operation PATCH on key {Key} requested at {RequestedAt}",
            command.Key,
            DateTimeOffset.UtcNow);

        return keyLockProvider.ExecuteWithLockAsync(
            command.Key,
            async token => await PatchAggregateAsync(command, token),
            cancellationToken);
    }

    private async Task<KeyValueResponse> PatchAggregateAsync(PatchKeyValueCommand command, CancellationToken token)
    {
        logger.LogInformation(
            "Operation PATCH on key {Key} starting at {ExecutionStart}",
            command.Key,
            DateTimeOffset.UtcNow);

        var aggregate = await repository.GetAsync(command.Key, token);

        if (aggregate is null)
        {
            if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != 0)
            {
                throw new VersionMismatchException(command.Key, command.ExpectedVersion.Value, 0);
            }

            aggregate = KeyValueAggregate.Create(command.Key, command.Delta);
        }
        else
        {
            aggregate.Merge(command.Delta, command.ExpectedVersion);
        }

        await repository.UpsertAsync(aggregate, token);
        return KeyValueResponse.FromSnapshot(aggregate.ToSnapshot());
    }
}

