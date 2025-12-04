using KvStore.Router.Clients;
using KvStore.Router.Models;
using KvStore.Router.Nodes;

namespace KvStore.Router.Services;

public sealed class KeyListingService(
    INodeRegistry nodeRegistry,
    IKvStoreNodeClient nodeClient) : IKeyListingService
{
    public async Task<IReadOnlyList<KeyListingRecord>> ListAsync(CancellationToken cancellationToken)
    {
        var nodes = nodeRegistry.Nodes;
        var tasks = nodes.Select(async node =>
        {
            var keys = await nodeClient.ListKeysAsync(node, cancellationToken);
            return keys.Select(key => new KeyListingRecord(key, node.Id)).ToArray();
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        return results
            .SelectMany(static records => records)
            .OrderBy(record => record.Node, StringComparer.Ordinal)
            .ThenBy(record => record.Key, StringComparer.Ordinal)
            .ToArray();
    }
}

