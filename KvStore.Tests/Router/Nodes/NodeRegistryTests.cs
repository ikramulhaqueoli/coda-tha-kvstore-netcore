using KvStore.Router.Nodes;
using KvStore.Router.Options;
using Microsoft.Extensions.Options;

namespace KvStore.Tests.Router.Nodes;

public sealed class NodeRegistryTests
{
    [Fact]
    public void Constructor_BuildsNodesFromReplicaTemplate()
    {
        var options = new KvStoreNodesOptions
        {
            ServiceName = "kvstore-api",
            HeadlessServiceName = "kvstore-api-headless",
            Namespace = "default",
            ClusterDomain = "svc.cluster.local",
            Port = 7000,
            Scheme = "http",
            ReplicaCount = 2
        };

        var registry = new NodeRegistry(Options.Create(options));

        Assert.Collection(
            registry.Nodes,
            node =>
            {
                Assert.Equal("kvstore-api-0", node.Id);
                Assert.Equal(new Uri("http://kvstore-api-0.kvstore-api-headless.default.svc.cluster.local:7000/"), node.BaseAddress);
            },
            node =>
            {
                Assert.Equal("kvstore-api-1", node.Id);
                Assert.Equal(new Uri("http://kvstore-api-1.kvstore-api-headless.default.svc.cluster.local:7000/"), node.BaseAddress);
            });
    }

    [Fact]
    public void Constructor_HonorsCustomHostTemplate()
    {
        var options = new KvStoreNodesOptions
        {
            ServiceName = "kvstore-api",
            HeadlessServiceName = "localhost",
            Port = 7061,
            Scheme = "https",
            ReplicaCount = 1,
            HostTemplate = "{headlessServiceName}"
        };

        var registry = new NodeRegistry(Options.Create(options));
        var node = Assert.Single(registry.Nodes);

        Assert.Equal("kvstore-api-0", node.Id);
        Assert.Equal(new Uri("https://localhost:7061/"), node.BaseAddress);
    }
}

