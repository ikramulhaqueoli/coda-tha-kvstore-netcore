using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace KvStore.Core.Application.Commands.PutKeyValue;

public sealed class PutKeyValueCommandHandler(
    IKeyValueRepository repository,
    IKeyLockProvider keyLockProvider,
    ILogger<PutKeyValueCommandHandler> logger) : ICommandHandler<PutKeyValueCommand, KeyValueResponse>
{
    public Task<KeyValueResponse> Handle(PutKeyValueCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Operation PUT on key {Key} requested at {RequestedAt}",
            command.Key,
            DateTimeOffset.UtcNow);

        return keyLockProvider.ExecuteWithLockAsync(
            command.Key,
            async token => await PutAggregateAsync(command, token),
            cancellationToken);
    }

    private async Task<KeyValueResponse> PutAggregateAsync(PutKeyValueCommand command, CancellationToken token)
    {
        logger.LogInformation(
            "Operation PUT on key {Key} starting at {ExecutionStart}",
            command.Key,
            DateTimeOffset.UtcNow);

        var aggregate = await repository.GetAsync(command.Key, token);

        if (aggregate is null)
        {
            if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != 0)
            {
                throw new VersionMismatchException(command.Key, command.ExpectedVersion.Value, 0);
            }

            aggregate = KeyValueAggregate.Create(command.Key, command.Value);
        }
        else
        {
            aggregate.Replace(command.Value, command.ExpectedVersion);
        }

        await repository.UpsertAsync(aggregate, token);
        return KeyValueResponse.FromSnapshot(aggregate.ToSnapshot());
    }
}

