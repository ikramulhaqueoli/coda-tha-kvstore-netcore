using KvStore.Router.Nodes;

namespace KvStore.Router.Partitioning;

public interface IKeyPartitioner
{
    NodeDefinition SelectNode(string key);
}

