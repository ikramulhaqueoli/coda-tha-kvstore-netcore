using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using KvStore.Router.Nodes;

namespace KvStore.Router.Partitioning;

/// <summary>
/// Consistent hashing ring with virtual nodes to minimize remapping as the node list changes.
/// </summary>
public sealed class ConsistentHashPartitioner : IKeyPartitioner
{
    private const int VirtualNodesPerPhysicalNode = 128;

    private static readonly IComparer<(uint Hash, NodeDefinition Node)> HashComparer =
        Comparer<(uint Hash, NodeDefinition Node)>.Create(static (left, right) =>
            left.Hash.CompareTo(right.Hash));

    private readonly ImmutableArray<(uint Hash, NodeDefinition Node)> ring;

    public ConsistentHashPartitioner(INodeRegistry nodeRegistry)
    {
        ArgumentNullException.ThrowIfNull(nodeRegistry);

        var nodes = nodeRegistry.Nodes ?? throw new InvalidOperationException("Node registry returned null.");
        if (nodes.Count == 0)
        {
            throw new InvalidOperationException("No nodes are registered.");
        }

        ring = nodes
            .SelectMany(CreateVirtualNodes)
            .OrderBy(tuple => tuple.Hash)
            .ToImmutableArray();

        if (ring.IsDefaultOrEmpty)
        {
            throw new InvalidOperationException("The hashing ring could not be built.");
        }
    }

    public NodeDefinition SelectNode(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key must be provided.", nameof(key));
        }

        var hash = ComputeHash(key);
        var searchResult = ring.BinarySearch((hash, null!), HashComparer);
        var index = searchResult >= 0 ? searchResult : ~searchResult;
        index = index < ring.Length ? index : 0;
        return ring[index].Node;
    }

    private static IEnumerable<(uint Hash, NodeDefinition Node)> CreateVirtualNodes(NodeDefinition node)
    {
        for (var i = 0; i < VirtualNodesPerPhysicalNode; i++)
        {
            var virtualKey = $"{node.Id}-vn-{i}";
            var hash = ComputeHash(virtualKey);
            yield return (hash, node);
        }
    }

    private static uint ComputeHash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = SHA256.HashData(bytes);
        return BitConverter.ToUInt32(hashBytes, 0);
    }
}


