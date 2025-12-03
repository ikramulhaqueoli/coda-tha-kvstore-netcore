using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using KvStore.Core.Domain.Validation;

namespace KvStore.Core.Application.KeyValue.Commands.PutKeyValue;

public sealed class PutKeyValueCommandHandler(
    IKeyValueRepository repository,
    IKeyLockProvider keyLockProvider) : ICommandHandler<PutKeyValueCommand, KeyValueResponse>
{
    public Task<KeyValueResponse> Handle(PutKeyValueCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        KeyValidator.EnsureValid(command.Key);

        return keyLockProvider.ExecuteWithLockAsync(command.Key, async token =>
        {
            var aggregate = await repository.GetAsync(command.Key, token);

            if (aggregate is null)
            {
                if (command.ExpectedVersion.HasValue)
                {
                    throw new VersionMismatchException(command.Key, command.ExpectedVersion.Value, null);
                }

                aggregate = KeyValueAggregate.Create(command.Key, command.Value);
            }
            else
            {
                aggregate.Replace(command.Value, command.ExpectedVersion);
            }

            await repository.UpsertAsync(aggregate, token);
            return KeyValueResponse.FromSnapshot(aggregate.ToSnapshot());
        }, cancellationToken);
    }
}

