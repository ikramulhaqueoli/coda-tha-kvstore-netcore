using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using KvStore.Router.Options;
using Microsoft.Extensions.Options;

namespace KvStore.Router.Nodes;

public sealed class NodeRegistry : INodeRegistry
{
    private readonly IReadOnlyList<NodeDefinition> _nodes;

    public NodeRegistry(IOptions<KvStoreNodesOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var value = options.Value ?? throw new ValidationException("KvStore node configuration is missing.");

        ValidateOptions(value);

        _nodes = Enumerable
            .Range(0, value.ReplicaCount)
            .Select(index => BuildNode(value, index))
            .ToArray();
    }

    public IReadOnlyList<NodeDefinition> Nodes => _nodes;

    private static void ValidateOptions(KvStoreNodesOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.StatefulSetName))
        {
            throw new ValidationException("StatefulSetName must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.HeadlessServiceName))
        {
            throw new ValidationException("HeadlessServiceName must be provided.");
        }

        if (options.ReplicaCount <= 0)
        {
            throw new ValidationException("ReplicaCount must be at least 1.");
        }

        if (options.Port is < 1 or > 65535)
        {
            throw new ValidationException("Port must be between 1 and 65535.");
        }
    }

    private static NodeDefinition BuildNode(KvStoreNodesOptions options, int replicaIndex)
    {
        var scheme = string.IsNullOrWhiteSpace(options.Scheme) ? "http" : options.Scheme;
        var statefulSetName = options.StatefulSetName.Trim();
        var nodeId = $"{statefulSetName}-{replicaIndex}";
        var host = BuildHost(options, nodeId, replicaIndex);
        var baseAddress = new Uri($"{scheme}://{host}:{options.Port}/", UriKind.Absolute);

        return new NodeDefinition(nodeId, baseAddress);
    }

    private static string BuildHost(KvStoreNodesOptions options, string nodeId, int replicaIndex)
    {
        var template = string.IsNullOrWhiteSpace(options.HostTemplate)
            ? KvStoreNodesOptions.DefaultHostTemplate
            : options.HostTemplate;

        var host = template
            .Replace("{nodeId}", nodeId, StringComparison.Ordinal)
            .Replace("{statefulSetName}", options.StatefulSetName, StringComparison.Ordinal)
            .Replace("{serviceName}", options.StatefulSetName, StringComparison.Ordinal)
            .Replace("{headlessServiceName}", options.HeadlessServiceName, StringComparison.Ordinal)
            .Replace("{namespace}", options.Namespace ?? string.Empty, StringComparison.Ordinal)
            .Replace("{clusterDomain}", options.ClusterDomain ?? string.Empty, StringComparison.Ordinal)
            .Replace("{replicaIndex}", replicaIndex.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

        host = NormalizeHost(host);

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ValidationException("Resolved node host cannot be empty.");
        }

        return host;
    }

    private static string NormalizeHost(string host)
    {
        var normalized = host.Trim();
        while (normalized.Contains("..", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("..", ".", StringComparison.Ordinal);
        }

        return normalized.Trim('.');
    }
}

