using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Commands.Responses;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using KvStore.Core.Domain.Validation;

namespace KvStore.Core.Application.Queries.GetKeyValue;

public sealed class GetKeyValueQueryHandler(
    IKeyValueRepository repository,
    IKeyLockProvider keyLockProvider) : IQueryHandler<GetKeyValueQuery, KeyValueResponse>
{
    public Task<KeyValueResponse> HandleAsync(GetKeyValueQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        KeyValidator.EnsureValid(query.Key);

        return keyLockProvider.ExecuteWithLockAsync(query.Key, async token =>
            await HandleGetKeyValueAggregateAsync(query, token), cancellationToken);
    }

    private async Task<KeyValueResponse> HandleGetKeyValueAggregateAsync(GetKeyValueQuery query, CancellationToken token)
    {
        var aggregate = await repository.GetAsync(query.Key, token);

        return aggregate is null
        ? throw new KeyValueNotFoundException(query.Key)
        : KeyValueResponse.FromSnapshot(aggregate.ToSnapshot());
    }
}

