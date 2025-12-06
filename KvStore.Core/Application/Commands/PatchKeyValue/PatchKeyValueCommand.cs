using System.Text.Json.Nodes;
using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Commands.Responses;

namespace KvStore.Core.Application.Commands.PatchKeyValue;

public sealed record PatchKeyValueCommand(string Key, JsonNode? Delta, int? ExpectedVersion)
    : ICommand<KeyValueResponse>;

