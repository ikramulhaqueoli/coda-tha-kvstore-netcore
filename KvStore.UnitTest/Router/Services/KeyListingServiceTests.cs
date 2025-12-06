using KvStore.Router.Clients;
using KvStore.Router.Models;
using KvStore.Router.Nodes;
using KvStore.Router.Services;

namespace KvStore.UnitTest.Router.Services;

public sealed class KeyListingServiceTests
{
    [Fact]
    public async Task ListAsync_AggregatesKeysFromAllNodes()
    {
        var nodes = new[]
        {
            new NodeDefinition("node-0", new Uri("http://node-0/")),
            new NodeDefinition("node-1", new Uri("http://node-1/")),
            new NodeDefinition("node-2", new Uri("http://node-2/"))
        };
        var registry = new TestNodeRegistry(nodes);
        var client = new TestNodeClient
        {
            KeysByNode = new Dictionary<string, string[]>
            {
                ["node-0"] = new[] { "key-a", "key-b" },
                ["node-1"] = new[] { "key-c" },
                ["node-2"] = new[] { "key-d", "key-e" }
            }
        };
        var service = new KeyListingService(registry, client);

        var result = await service.ListAsync(CancellationToken.None);

        Assert.Equal(5, result.Count);
        Assert.Contains(result, r => r.Key == "key-a" && r.Node == "node-0");
        Assert.Contains(result, r => r.Key == "key-b" && r.Node == "node-0");
        Assert.Contains(result, r => r.Key == "key-c" && r.Node == "node-1");
        Assert.Contains(result, r => r.Key == "key-d" && r.Node == "node-2");
        Assert.Contains(result, r => r.Key == "key-e" && r.Node == "node-2");
    }

    [Fact]
    public async Task ListAsync_ReturnsSortedResults()
    {
        var nodes = new[]
        {
            new NodeDefinition("node-1", new Uri("http://node-1/")),
            new NodeDefinition("node-0", new Uri("http://node-0/"))
        };
        var registry = new TestNodeRegistry(nodes);
        var client = new TestNodeClient
        {
            KeysByNode = new Dictionary<string, string[]>
            {
                ["node-0"] = new[] { "z-key" },
                ["node-1"] = new[] { "a-key" }
            }
        };
        var service = new KeyListingService(registry, client);

        var result = (await service.ListAsync(CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        // Results are sorted by Node first, then Key
        Assert.True(result.All(r => r.Node == "node-0" || r.Node == "node-1"));
        Assert.Contains(result, r => r.Key == "a-key" && r.Node == "node-1");
        Assert.Contains(result, r => r.Key == "z-key" && r.Node == "node-0");
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoNodes()
    {
        var registry = new TestNodeRegistry(Array.Empty<NodeDefinition>());
        var client = new TestNodeClient();
        var service = new KeyListingService(registry, client);

        var result = await service.ListAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_HandlesEmptyNodeResponses()
    {
        var nodes = new[]
        {
            new NodeDefinition("node-0", new Uri("http://node-0/")),
            new NodeDefinition("node-1", new Uri("http://node-1/"))
        };
        var registry = new TestNodeRegistry(nodes);
        var client = new TestNodeClient
        {
            KeysByNode = new Dictionary<string, string[]>
            {
                ["node-0"] = Array.Empty<string>(),
                ["node-1"] = new[] { "key-1" }
            }
        };
        var service = new KeyListingService(registry, client);

        var result = await service.ListAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("key-1", result[0].Key);
    }

    private sealed class TestNodeRegistry : INodeRegistry
    {
        public TestNodeRegistry(IReadOnlyList<NodeDefinition> nodes)
        {
            Nodes = nodes;
        }

        public IReadOnlyList<NodeDefinition> Nodes { get; }
    }

    private sealed class TestNodeClient : IKvStoreNodeClient
    {
        public Dictionary<string, string[]> KeysByNode { get; set; } = new();

        public Task<KeyValueRecord> GetAsync(NodeDefinition node, string key, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<KeyValueRecord> PutAsync(
            NodeDefinition node,
            string key,
            System.Text.Json.Nodes.JsonNode? payload,
            int? expectedVersion,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<KeyValueRecord> PatchAsync(
            NodeDefinition node,
            string key,
            System.Text.Json.Nodes.JsonNode? payload,
            int? expectedVersion,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> ListKeysAsync(NodeDefinition node, CancellationToken cancellationToken)
        {
            var keys = KeysByNode.TryGetValue(node.Id, out var nodeKeys) ? nodeKeys : Array.Empty<string>();
            return Task.FromResult<IReadOnlyCollection<string>>(keys);
        }
    }
}

