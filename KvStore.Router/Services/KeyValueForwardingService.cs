using System.Text.Json.Nodes;
using KvStore.Router.Clients;
using KvStore.Router.Models;
using KvStore.Router.Partitioning;

namespace KvStore.Router.Services;

public sealed class KeyValueForwardingService(
    IKeyPartitioner partitioner,
    IKvStoreNodeClient nodeClient) : IKeyValueForwardingService
{
    public Task<KeyValueRecord> GetAsync(string key, CancellationToken cancellationToken)
    {
        var node = partitioner.SelectNode(key);
        return nodeClient.GetAsync(node, key, cancellationToken);
    }

    public async Task<KeyValueRecord> PutAsync(string key, JsonNode? payload, long? expectedVersion, CancellationToken cancellationToken)
    {
        var node = partitioner.SelectNode(key);
        return await nodeClient.PutAsync(node, key, payload, expectedVersion, cancellationToken);
    }

    public async Task<KeyValueRecord> PatchAsync(string key, JsonNode? payload, long? expectedVersion, CancellationToken cancellationToken)
    {
        var node = partitioner.SelectNode(key);
        return await nodeClient.PatchAsync(node, key, payload, expectedVersion, cancellationToken);
    }
}

