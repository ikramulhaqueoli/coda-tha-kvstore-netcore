using KvStore.Core.Application.Abstractions;

namespace KvStore.Core.Application.KeyValue.Queries.ListKeys;

public sealed record ListKeysQuery : IQuery<IReadOnlyCollection<string>>;

