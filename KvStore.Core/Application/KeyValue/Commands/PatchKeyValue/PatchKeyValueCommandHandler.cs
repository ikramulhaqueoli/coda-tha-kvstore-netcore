using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using KvStore.Core.Domain.Validation;

namespace KvStore.Core.Application.KeyValue.Commands.PatchKeyValue;

public sealed class PatchKeyValueCommandHandler(
    IKeyValueRepository repository,
    IKeyLockProvider keyLockProvider) : ICommandHandler<PatchKeyValueCommand, KeyValueResponse>
{
    public Task<KeyValueResponse> Handle(PatchKeyValueCommand command, CancellationToken cancellationToken)
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

                aggregate = KeyValueAggregate.Create(command.Key, command.Delta);
            }
            else
            {
                aggregate.Merge(command.Delta, command.ExpectedVersion);
            }

            await repository.UpsertAsync(aggregate, token);
            return KeyValueResponse.FromSnapshot(aggregate.ToSnapshot());
        }, cancellationToken);
    }
}

