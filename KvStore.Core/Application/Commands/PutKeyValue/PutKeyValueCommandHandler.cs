using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;

namespace KvStore.Core.Application.Commands.PutKeyValue;

public sealed class PutKeyValueCommandHandler(
    IKeyValueRepository repository,
    IKeyLockProvider keyLockProvider) : ICommandHandler<PutKeyValueCommand, KeyValueResponse>
{
    public Task<KeyValueResponse> HandleAsync(PutKeyValueCommand command, CancellationToken cancellationToken)
    {
        return keyLockProvider.ExecuteWithLockAsync(
            command.Key,
            async token => await HandlePutAggregateAsync(command, token),
            cancellationToken);
    }

    private async Task<KeyValueResponse> HandlePutAggregateAsync(PutKeyValueCommand command, CancellationToken token)
    {
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

