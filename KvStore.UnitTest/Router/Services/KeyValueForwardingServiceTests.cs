using System.Text.Json.Nodes;
using KvStore.Router.Clients;
using KvStore.Router.Models;
using KvStore.Router.Nodes;
using KvStore.Router.Partitioning;
using KvStore.Router.Services;

namespace KvStore.UnitTest.Router.Services;

public sealed class KeyValueForwardingServiceTests
{
    [Fact]
    public async Task GetAsync_ForwardsToCorrectNode()
    {
        var node = new NodeDefinition("node-1", new Uri("http://node-1/"));
        var partitioner = new TestPartitioner(node);
        var client = new TestNodeClient();
        var service = new KeyValueForwardingService(partitioner, client);

        var result = await service.GetAsync("test-key", CancellationToken.None);

        Assert.Equal("test-key", result.Record.Key);
        Assert.Equal("node-1", result.NodeId);
        Assert.Equal("test-key", client.GetKey);
        Assert.Equal(node, client.GetNode);
    }

    [Fact]
    public async Task PutAsync_ForwardsToCorrectNode()
    {
        var node = new NodeDefinition("node-2", new Uri("http://node-2/"));
        var partitioner = new TestPartitioner(node);
        var client = new TestNodeClient();
        var service = new KeyValueForwardingService(partitioner, client);

        var payload = JsonValue.Create(42);
        var result = await service.PutAsync("test-key", payload, expectedVersion: 1, CancellationToken.None);

        Assert.Equal("test-key", result.Record.Key);
        Assert.Equal("node-2", result.NodeId);
        Assert.Equal("test-key", client.PutKey);
        Assert.Equal(payload, client.PutPayload);
        Assert.Equal(1, client.PutExpectedVersion);
    }

    [Fact]
    public async Task PatchAsync_ForwardsToCorrectNode()
    {
        var node = new NodeDefinition("node-3", new Uri("http://node-3/"));
        var partitioner = new TestPartitioner(node);
        var client = new TestNodeClient();
        var service = new KeyValueForwardingService(partitioner, client);

        var delta = JsonNode.Parse("""{"count":1}""");
        var result = await service.PatchAsync("test-key", delta, expectedVersion: null, CancellationToken.None);

        Assert.Equal("test-key", result.Record.Key);
        Assert.Equal("node-3", result.NodeId);
        Assert.Equal("test-key", client.PatchKey);
        Assert.Equal(delta, client.PatchPayload);
        Assert.Null(client.PatchExpectedVersion);
    }

    [Fact]
    public async Task GetAsync_MeasuresExecutionTime()
    {
        var node = new NodeDefinition("node-1", new Uri("http://node-1/"));
        var partitioner = new TestPartitioner(node);
        var client = new TestNodeClient { Delay = TimeSpan.FromMilliseconds(50) };
        var service = new KeyValueForwardingService(partitioner, client);

        var result = await service.GetAsync("test-key", CancellationToken.None);

        Assert.True(result.ExecutionTime.TotalMilliseconds >= 50);
    }

    private sealed class TestPartitioner : IKeyPartitioner
    {
        private readonly NodeDefinition _node;

        public TestPartitioner(NodeDefinition node)
        {
            _node = node;
        }

        public NodeDefinition SelectNode(string key) => _node;
    }

    private sealed class TestNodeClient : IKvStoreNodeClient
    {
        public TimeSpan Delay { get; set; }
        public string? GetKey { get; private set; }
        public NodeDefinition? GetNode { get; private set; }
        public string? PutKey { get; private set; }
        public JsonNode? PutPayload { get; private set; }
        public int? PutExpectedVersion { get; private set; }
        public string? PatchKey { get; private set; }
        public JsonNode? PatchPayload { get; private set; }
        public int? PatchExpectedVersion { get; private set; }

        public async Task<KeyValueRecord> GetAsync(NodeDefinition node, string key, CancellationToken cancellationToken)
        {
            await Task.Delay(Delay, cancellationToken);
            GetKey = key;
            GetNode = node;
            return new KeyValueRecord(key, JsonValue.Create(42), 1);
        }

        public async Task<KeyValueRecord> PutAsync(
            NodeDefinition node,
            string key,
            JsonNode? payload,
            int? expectedVersion,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Delay, cancellationToken);
            PutKey = key;
            PutPayload = payload;
            PutExpectedVersion = expectedVersion;
            return new KeyValueRecord(key, payload, 1);
        }

        public async Task<KeyValueRecord> PatchAsync(
            NodeDefinition node,
            string key,
            JsonNode? payload,
            int? expectedVersion,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Delay, cancellationToken);
            PatchKey = key;
            PatchPayload = payload;
            PatchExpectedVersion = expectedVersion;
            return new KeyValueRecord(key, payload, 2);
        }

        public Task<IReadOnlyCollection<string>> ListKeysAsync(NodeDefinition node, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }
    }
}

