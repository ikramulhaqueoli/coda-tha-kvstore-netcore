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

    private sealed class TestNodeRegistry : INodeRegistry
    {
        public TestNodeRegistry(IReadOnlyList<NodeDefinition> nodes)
        {
            Nodes = nodes;
        }

        public IReadOnlyList<NodeDefinition> Nodes { get; }
    }
}


