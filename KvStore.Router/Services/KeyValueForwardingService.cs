using System.Diagnostics;
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
        var stopwatch = Stopwatch.StartNew();
        var record = await nodeClient.GetAsync(node, key, cancellationToken);
        stopwatch.Stop();
        return new ForwardedKeyValueResult(record, node.Id, stopwatch.Elapsed);
    }

    public async Task<ForwardedKeyValueResult> PutAsync(
        string key,
        JsonNode? payload,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var node = partitioner.SelectNode(key);
        var stopwatch = Stopwatch.StartNew();
        var record = await nodeClient.PutAsync(node, key, payload, expectedVersion, cancellationToken);
        stopwatch.Stop();
        return new ForwardedKeyValueResult(record, node.Id, stopwatch.Elapsed);
    }

    public async Task<ForwardedKeyValueResult> PatchAsync(
        string key,
        JsonNode? payload,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var node = partitioner.SelectNode(key);
        var stopwatch = Stopwatch.StartNew();
        var record = await nodeClient.PatchAsync(node, key, payload, expectedVersion, cancellationToken);
        stopwatch.Stop();
        return new ForwardedKeyValueResult(record, node.Id, stopwatch.Elapsed);
    }
}

