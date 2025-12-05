using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Repositories;
using KvStore.Core.Domain.Validation;
using Microsoft.Extensions.Logging;

namespace KvStore.Core.Application.Queries.GetKeyValue;

public sealed class GetKeyValueQueryHandler(
    IKeyValueRepository repository,
    IKeyLockProvider keyLockProvider,
    ILogger<GetKeyValueQueryHandler> logger) : IQueryHandler<GetKeyValueQuery, KeyValueResponse>
{
    public Task<KeyValueResponse> Handle(GetKeyValueQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        KeyValidator.EnsureValid(query.Key);

        logger.LogInformation(
            "Operation GET on key {Key} requested at {RequestedAt}",
            query.Key,
            DateTimeOffset.UtcNow);

        return keyLockProvider.ExecuteWithLockAsync(query.Key, async token =>
            await GetKeyValueAggregateAsync(query, token), cancellationToken);
    }

    private async Task<KeyValueResponse> GetKeyValueAggregateAsync(GetKeyValueQuery query, CancellationToken token)
    {
        logger.LogInformation(
            "Operation GET on key {Key} starting at {ExecutionStart}",
            query.Key,
            DateTimeOffset.UtcNow);

        var aggregate = await repository.GetAsync(query.Key, token);

        return aggregate is null
        ? throw new KeyValueNotFoundException(query.Key)
        : KeyValueResponse.FromSnapshot(aggregate.ToSnapshot());
    }
}

