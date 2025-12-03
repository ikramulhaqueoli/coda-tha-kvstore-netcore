using System.Text.Json.Nodes;
using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.KeyValue.Responses;

namespace KvStore.Core.Application.KeyValue.Commands.PutKeyValue;

public sealed record PutKeyValueCommand(string Key, JsonNode? Value, long? ExpectedVersion)
    : ICommand<KeyValueResponse>;

