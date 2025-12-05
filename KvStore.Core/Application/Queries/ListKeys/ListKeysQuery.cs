using KvStore.Core.Application.Abstractions;

namespace KvStore.Core.Application.Queries.ListKeys;

public sealed record ListKeysQuery : IQuery<IReadOnlyCollection<string>>;

