using KvStore.Router.Nodes;
using KvStore.Router.Partitioning;

namespace KvStore.Tests.Router.Partitioning;

public sealed class ConsistentHashPartitionerTests
{
    [Fact]
    public void SelectNode_ReturnsSameNodeForSameKey()
    {
        var registry = new StubNodeRegistry(
            new NodeDefinition("node-a", new Uri("http://node-a")),
            new NodeDefinition("node-b", new Uri("http://node-b")),
            new NodeDefinition("node-c", new Uri("http://node-c")));

        var partitioner = new ConsistentHashPartitioner(registry);

        var first = partitioner.SelectNode("user:42");
        var second = partitioner.SelectNode("user:42");

        Assert.Equal(first, second);
    }

    [Fact]
    public void SelectNode_DistributesKeysAcrossNodes()
    {
        var registry = new StubNodeRegistry(
            new NodeDefinition("node-a", new Uri("http://node-a")),
            new NodeDefinition("node-b", new Uri("http://node-b")),
            new NodeDefinition("node-c", new Uri("http://node-c")));

        var partitioner = new ConsistentHashPartitioner(registry);

        var nodes = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < 50; i++)
        {
            var node = partitioner.SelectNode($"user:{i}");
            nodes.Add(node.Id);
            if (nodes.Count >= 2)
            {
                break;
            }
        }

        Assert.True(nodes.Count >= 2, "Keys should spread across at least two nodes.");
    }

    [Fact]
    public void SelectNode_ThrowsWhenKeyMissing()
    {
        var registry = new StubNodeRegistry(new NodeDefinition("node-a", new Uri("http://node-a")));
        var partitioner = new ConsistentHashPartitioner(registry);

        Assert.Throws<ArgumentException>(() => partitioner.SelectNode(" "));
    }

    [Fact]
    public void Constructor_ThrowsWhenNoNodesRegistered()
    {
        var registry = new StubNodeRegistry(Array.Empty<NodeDefinition>());
        Assert.Throws<InvalidOperationException>(() => new ConsistentHashPartitioner(registry));
    }

    private sealed class StubNodeRegistry : INodeRegistry
    {
        public StubNodeRegistry(params NodeDefinition[] nodes)
        {
            Nodes = nodes;
        }

        public IReadOnlyList<NodeDefinition> Nodes { get; }
    }
}


