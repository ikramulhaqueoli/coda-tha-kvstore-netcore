using KvStore.Router.Nodes;
using KvStore.Router.Partitioning;

namespace KvStore.UnitTest.Router.Partitioning;

public sealed class ConsistentHashPartitionerTests
{
    [Fact]
    public void Constructor_Throws_WhenRegistryHasNoNodes()
    {
        var registry = new TestNodeRegistry([]);

        Assert.Throws<InvalidOperationException>(() => new ConsistentHashPartitioner(registry));
    }

    [Fact]
    public void SelectNode_ReturnsSameNode_ForSameKey()
    {
        var nodes = new[]
        {
            new NodeDefinition("node-0", new Uri("http://node-0/")),
            new NodeDefinition("node-1", new Uri("http://node-1/")),
            new NodeDefinition("node-2", new Uri("http://node-2/"))
        };

        var registry = new TestNodeRegistry(nodes);
        var partitioner = new ConsistentHashPartitioner(registry);

        var first = partitioner.SelectNode("user:42");
        var second = partitioner.SelectNode("user:42");

        Assert.Equal(first, second);
        Assert.Contains(first, nodes);
    }

    [Fact]
    public void SelectNode_DistributesKeysAcrossNodes()
    {
        var nodes = new[]
        {
            new NodeDefinition("node-0", new Uri("http://node-0/")),
            new NodeDefinition("node-1", new Uri("http://node-1/")),
            new NodeDefinition("node-2", new Uri("http://node-2/"))
        };

        var registry = new TestNodeRegistry(nodes);
        var partitioner = new ConsistentHashPartitioner(registry);

        var keys = new[] { "key1", "key2", "key3", "key4", "key5", "key6", "key7", "key8", "key9", "key10" };
        var selectedNodes = keys.Select(k => partitioner.SelectNode(k)).ToList();

        var uniqueNodes = selectedNodes.Distinct().ToList();
        Assert.True(uniqueNodes.Count > 1, "Keys should be distributed across multiple nodes");
    }

    [Fact]
    public void SelectNode_WithSingleNode_AlwaysReturnsThatNode()
    {
        var nodes = new[]
        {
            new NodeDefinition("node-0", new Uri("http://node-0/"))
        };

        var registry = new TestNodeRegistry(nodes);
        var partitioner = new ConsistentHashPartitioner(registry);

        var node1 = partitioner.SelectNode("key1");
        var node2 = partitioner.SelectNode("key2");
        var node3 = partitioner.SelectNode("key3");

        Assert.Equal(nodes[0], node1);
        Assert.Equal(nodes[0], node2);
        Assert.Equal(nodes[0], node3);
    }

    [Fact]
    public void SelectNode_ConsistentAcrossMultipleCalls()
    {
        var nodes = new[]
        {
            new NodeDefinition("node-0", new Uri("http://node-0/")),
            new NodeDefinition("node-1", new Uri("http://node-1/")),
            new NodeDefinition("node-2", new Uri("http://node-2/"))
        };

        var registry = new TestNodeRegistry(nodes);
        var partitioner1 = new ConsistentHashPartitioner(registry);
        var partitioner2 = new ConsistentHashPartitioner(registry);

        var keys = new[] { "key1", "key2", "key3", "key4", "key5" };
        foreach (var key in keys)
        {
            var node1 = partitioner1.SelectNode(key);
            var node2 = partitioner2.SelectNode(key);
            Assert.Equal(node1, node2);
        }
    }

    private sealed class TestNodeRegistry : INodeRegistry
    {
        public TestNodeRegistry(IReadOnlyList<NodeDefinition> nodes)
        {
            Nodes = nodes;
        }

        public IReadOnlyList<NodeDefinition> Nodes { get; }
    }
}


