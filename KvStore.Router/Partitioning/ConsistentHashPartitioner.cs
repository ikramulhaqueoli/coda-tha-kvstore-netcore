using System.Security.Cryptography;
using System.Text;
using KvStore.Router.Nodes;

namespace KvStore.Router.Partitioning;

public sealed class ConsistentHashPartitioner(INodeRegistry nodeRegistry) : IKeyPartitioner
{
    public NodeDefinition SelectNode(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key must be provided.", nameof(key));
        }

        var nodes = nodeRegistry.Nodes;
        if (nodes.Count == 0)
        {
            throw new InvalidOperationException("No nodes are registered.");
        }

        var hash = ComputeHash(key);
        var index = (int)(hash % (uint)nodes.Count);
        return nodes[index];
    }

    private static uint ComputeHash(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hashBytes = SHA256.HashData(bytes);
        return BitConverter.ToUInt32(hashBytes, 0);
    }
}

