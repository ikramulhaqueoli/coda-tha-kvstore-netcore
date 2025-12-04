namespace KvStore.Router.Nodes;

public interface INodeRegistry
{
    IReadOnlyList<NodeDefinition> Nodes { get; }
}

