using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using KvStore.Core.Domain.Validation;

namespace KvStore.Core.Application.KeyValue.Queries.GetKeyValue;

public sealed class GetKeyValueQueryHandler(
    IKeyValueRepository repository,
    IKeyLockProvider keyLockProvider) : IQueryHandler<GetKeyValueQuery, KeyValueResponse>
{
    public Task<KeyValueResponse> Handle(GetKeyValueQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        KeyValidator.EnsureValid(query.Key);

        return keyLockProvider.ExecuteWithLockAsync(query.Key, async token =>
        {
            var aggregate = await repository.GetAsync(query.Key, token);

            return aggregate is null 
            ? throw new KeyValueNotFoundException(query.Key)
            : KeyValueResponse.FromSnapshot(aggregate.ToSnapshot());
        }, cancellationToken);
    }
}

