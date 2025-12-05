using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.KeyValue.Responses;

namespace KvStore.Core.Application.Queries.GetKeyValue;

public sealed record GetKeyValueQuery(string Key) : IQuery<KeyValueResponse>;

