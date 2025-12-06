using KvStore.Core.Application.Abstractions;
using KvStore.Core.Domain.Repositories;

namespace KvStore.Core.Application.Queries.ListKeys;

public sealed class ListKeysQueryHandler(IKeyValueRepository repository)
    : IQueryHandler<ListKeysQuery, IReadOnlyCollection<string>>
{
    public Task<IReadOnlyCollection<string>> HandleAsync(ListKeysQuery query, CancellationToken cancellationToken)
        => repository.ListKeysAsync(cancellationToken);
}

