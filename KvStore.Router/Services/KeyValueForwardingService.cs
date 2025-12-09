using System.Diagnostics;
using System.Text.Json.Nodes;
using KvStore.Router.Clients;
using KvStore.Router.Models;
using KvStore.Router.Partitioning;

namespace KvStore.Router.Services;

public sealed class KeyValueForwardingService(
    IKeyPartitioner partitioner,
    IKvStoreNodeClient nodeClient,
    IHttpContextAccessor httpContextAccessor,
    ILogger<KeyValueForwardingService> logger) : IKeyValueForwardingService
{
    public async Task<ForwardedKeyValueResult> GetAsync(string key, CancellationToken cancellationToken)
    {
        var node = partitioner.SelectNode(key);
        LogNodeSelection("GET", key, node.Id);
        var stopwatch = Stopwatch.StartNew();
        var record = await nodeClient.GetAsync(node, key, cancellationToken);
        stopwatch.Stop();
        LogRequestCompletion("GET", key, node.Id, stopwatch.ElapsedMilliseconds);
        return new ForwardedKeyValueResult(record, node.Id, stopwatch.Elapsed);
    }

    public async Task<ForwardedKeyValueResult> PutAsync(
        string key,
        JsonNode? payload,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var node = partitioner.SelectNode(key);
        LogNodeSelection("PUT", key, node.Id);
        var stopwatch = Stopwatch.StartNew();
        var record = await nodeClient.PutAsync(node, key, payload, expectedVersion, cancellationToken);
        stopwatch.Stop();
        LogRequestCompletion("PUT", key, node.Id, stopwatch.ElapsedMilliseconds);
        return new ForwardedKeyValueResult(record, node.Id, stopwatch.Elapsed);
    }

    public async Task<ForwardedKeyValueResult> PatchAsync(
        string key,
        JsonNode? payload,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var node = partitioner.SelectNode(key);
        LogNodeSelection("PATCH", key, node.Id);
        var stopwatch = Stopwatch.StartNew();
        var record = await nodeClient.PatchAsync(node, key, payload, expectedVersion, cancellationToken);
        stopwatch.Stop();
        LogRequestCompletion("PATCH", key, node.Id, stopwatch.ElapsedMilliseconds);

        return new ForwardedKeyValueResult(record, node.Id, stopwatch.Elapsed);
    }

    private void LogNodeSelection(string method, string key, string nodeId)
    {
        var requestHash = httpContextAccessor.HttpContext?.Request.GetHashCode() ?? 0;
        
        logger.LogInformation(
            "Request #{RequestHash}: {Method} key {Key} request being forwarded to Node: {NodeId}",
            requestHash,
            method,
            key,
            nodeId);
    }

    private void LogRequestCompletion(string method, string key, string nodeId, long executionTimeMs)
    {
        var requestHash = httpContextAccessor.HttpContext?.Request.GetHashCode() ?? 0;

        logger.LogInformation(
            "Request #{RequestHash}: {Method} key {Key} completed in {ExecutionTimeMs} ms by Node: {NodeId}",
            requestHash,
            method,
            key,
            executionTimeMs,
            nodeId);
    }
}

