using System.Collections.Concurrent;
using KvStore.Core.Domain.Entities;
using KvStore.Core.Domain.Repositories;

namespace KvStore.Infrastructure.Persistence;

public sealed class InMemoryKeyValueRepository : IKeyValueRepository
{
    private readonly ConcurrentDictionary<string, KeyValueAggregate> _store = new(StringComparer.Ordinal);

    public Task<KeyValueAggregate?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.TryGetValue(key, out var aggregate) ? aggregate.Clone() : null);
    }

    public Task UpsertAsync(KeyValueAggregate aggregate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store[aggregate.Key] = aggregate.Clone();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<string>> ListKeysAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<string>>(_store.Keys.ToList());
    }
}

