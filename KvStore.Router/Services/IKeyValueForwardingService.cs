using System.Text.Json.Nodes;
using KvStore.Router.Models;

namespace KvStore.Router.Services;

public interface IKeyValueForwardingService
{
    Task<ForwardedKeyValueResult> GetAsync(string key, CancellationToken cancellationToken);

    Task<ForwardedKeyValueResult> PutAsync(
        string key,
        JsonNode? payload,
        int? expectedVersion,
        CancellationToken cancellationToken);

    Task<ForwardedKeyValueResult> PatchAsync(
        string key,
        JsonNode? payload,
        int? expectedVersion,
        CancellationToken cancellationToken);
}

