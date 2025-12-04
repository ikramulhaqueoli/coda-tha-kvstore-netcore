using System.Net;
using KvStore.Router.Nodes;

namespace KvStore.Router.Clients;

public sealed class NodeHttpException(NodeDefinition node, HttpStatusCode statusCode, string? responseBody)
    : Exception($"Node '{node.Id}' responded with {(int)statusCode} ({statusCode}).")
{
    public NodeDefinition Node { get; } = node;
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? ResponseBody { get; } = responseBody;
}

