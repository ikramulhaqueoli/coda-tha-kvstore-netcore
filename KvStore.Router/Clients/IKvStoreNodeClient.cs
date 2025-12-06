using System.Text.Json.Nodes;
using KvStore.Router.Models;
using KvStore.Router.Nodes;

namespace KvStore.Router.Clients;

public interface IKvStoreNodeClient
{
    Task<KeyValueRecord> GetAsync(
        NodeDefinition node,
        string key,
        CancellationToken cancellationToken);

    Task<KeyValueRecord> PutAsync(
        NodeDefinition node,
        string key,
        JsonNode? payload,
        int? expectedVersion,
        CancellationToken cancellationToken);

    Task<KeyValueRecord> PatchAsync(
        NodeDefinition node,
        string key,
        JsonNode? payload,
        int? expectedVersion,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> ListKeysAsync(
        NodeDefinition node,
        CancellationToken cancellationToken);
}

