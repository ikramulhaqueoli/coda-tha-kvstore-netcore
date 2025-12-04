using System.Text.Json.Nodes;
using KvStore.Router.Clients;
using KvStore.Router.Models;
using KvStore.Router.Partitioning;

namespace KvStore.Router.Services;

public sealed class KeyValueForwardingService(
    IKeyPartitioner partitioner,
    IKvStoreNodeClient nodeClient) : IKeyValueForwardingService
{
    public async Task<ForwardedKeyValueResult> GetAsync(string key, CancellationToken cancellationToken)
    {
        var node = partitioner.SelectNode(key);
        var record = await nodeClient.GetAsync(node, key, cancellationToken);
        return new ForwardedKeyValueResult(record, node.Id);
    }

    public async Task<ForwardedKeyValueResult> PutAsync(
        string key,
        JsonNode? payload,
        long? expectedVersion,
        CancellationToken cancellationToken)
    {
        var node = partitioner.SelectNode(key);
        var record = await nodeClient.PutAsync(node, key, payload, expectedVersion, cancellationToken);
        return new ForwardedKeyValueResult(record, node.Id);
    }

    public async Task<ForwardedKeyValueResult> PatchAsync(
        string key,
        JsonNode? payload,
        long? expectedVersion,
        CancellationToken cancellationToken)
    {
        var node = partitioner.SelectNode(key);
        var record = await nodeClient.PatchAsync(node, key, payload, expectedVersion, cancellationToken);
        return new ForwardedKeyValueResult(record, node.Id);
    }
}

