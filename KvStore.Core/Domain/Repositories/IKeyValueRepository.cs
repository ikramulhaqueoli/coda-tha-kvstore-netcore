using KvStore.Core.Domain.Entities;

namespace KvStore.Core.Domain.Repositories;

public interface IKeyValueRepository
{
    Task<KeyValueAggregate?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task UpsertAsync(KeyValueAggregate aggregate, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> ListKeysAsync(CancellationToken cancellationToken = default);
}

