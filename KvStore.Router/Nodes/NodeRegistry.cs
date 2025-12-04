using System.ComponentModel.DataAnnotations;
using KvStore.Router.Options;
using Microsoft.Extensions.Options;

namespace KvStore.Router.Nodes;

public sealed class NodeRegistry : INodeRegistry
{
    private readonly IReadOnlyList<NodeDefinition> _nodes;

    public NodeRegistry(IOptions<KvStoreNodesOptions> options)
    {
        var configuredNodes = options.Value.Nodes;
        if (configuredNodes is null || configuredNodes.Count == 0)
        {
            throw new ValidationException("At least one KvStore node must be configured.");
        }

        var builtNodes = configuredNodes
            .Select(node => new NodeDefinition(
                node.Id ?? throw new ValidationException("Each node must declare an id."),
                node.BuildBaseAddress()))
            .ToArray();

        if (builtNodes.Select(n => n.Id).Distinct(StringComparer.Ordinal).Count() != builtNodes.Length)
        {
            throw new ValidationException("Duplicate node identifiers are not allowed.");
        }

        _nodes = builtNodes;
    }

    public IReadOnlyList<NodeDefinition> Nodes => _nodes;
}

