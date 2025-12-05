using System.ComponentModel.DataAnnotations;
using KvStore.Router.Nodes;
using KvStore.Router.Options;
using Microsoft.Extensions.Options;

namespace KvStore.UnitTest.Router.Nodes;

public sealed class NodeRegistryTests
{
    [Fact]
    public void Constructor_BuildsExpectedNodeMetadata()
    {
        var options = Options.Create(new KvStoreNodesOptions
        {
            StatefulSetName = "kvstore",
            HeadlessServiceName = "kvstore-hl",
            ReplicaCount = 2,
            Port = 8443,
            Scheme = "https",
            Namespace = "prod",
            ClusterDomain = "cluster.local",
            HostTemplate = "{nodeId}..{headlessServiceName}.{namespace}.{clusterDomain}."
        });

        var registry = new NodeRegistry(options);

        Assert.Equal(2, registry.Nodes.Count);
        var node = registry.Nodes[1];
        Assert.Equal("kvstore-1", node.Id);
        Assert.Equal(new Uri("https://kvstore-1.kvstore-hl.prod.cluster.local:8443/"), node.BaseAddress);
    }

    [Fact]
    public void Constructor_ValidatesReplicaCount()
    {
        var options = Options.Create(new KvStoreNodesOptions
        {
            StatefulSetName = "kvstore",
            HeadlessServiceName = "kvstore-hl",
            ReplicaCount = 0
        });

        Assert.Throws<ValidationException>(() => new NodeRegistry(options));
    }
}


