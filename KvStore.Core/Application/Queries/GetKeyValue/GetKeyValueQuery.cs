using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Commands.Responses;

namespace KvStore.Core.Application.Queries.GetKeyValue;

public sealed record GetKeyValueQuery(string Key) : IQuery<KeyValueResponse>;

