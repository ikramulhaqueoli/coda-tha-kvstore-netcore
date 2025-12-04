using KvStore.Router.Nodes;

namespace KvStore.Router.Clients;

public sealed class NodeUnavailableException(NodeDefinition node, Exception innerException)
    : Exception($"Unable to reach node '{node.Id}' at {node.BaseAddress}.", innerException)
{
    public NodeDefinition Node { get; } = node;
}

