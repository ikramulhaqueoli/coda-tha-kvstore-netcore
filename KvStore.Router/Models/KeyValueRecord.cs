using System.Text.Json.Nodes;

namespace KvStore.Router.Models;

public sealed record KeyValueRecord(string Key, JsonNode? Value, int Version);

