using System.Text.Json.Nodes;
using KvStore.Core.Domain.Entities;

namespace KvStore.Core.Application.Commands.Responses;

public sealed record KeyValueResponse(string Key, JsonNode? Value, int Version)
{
    public static KeyValueResponse FromSnapshot(KeyValueResponseSnapshot snapshot)
        => new(snapshot.Key, snapshot.Value?.DeepClone(), snapshot.Version);
}

