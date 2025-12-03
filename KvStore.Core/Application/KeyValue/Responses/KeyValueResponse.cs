using System.Text.Json.Nodes;
using KvStore.Core.Domain.Entities;

namespace KvStore.Core.Application.KeyValue.Responses;

public sealed record KeyValueResponse(string Key, JsonNode? Value, long Version)
{
    public static KeyValueResponse FromSnapshot(KeyValueResponseSnapshot snapshot)
        => new(snapshot.Key, snapshot.Value?.DeepClone(), snapshot.Version);
}

