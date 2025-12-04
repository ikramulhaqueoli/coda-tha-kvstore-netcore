using System.Text.Json.Nodes;
using KvStore.Router.Models;

namespace KvStore.Router.Services;

public interface IKeyValueForwardingService
{
    Task<KeyValueRecord> GetAsync(string key, CancellationToken cancellationToken);

    Task<KeyValueRecord> PutAsync(string key, JsonNode? payload, long? expectedVersion, CancellationToken cancellationToken);

    Task<KeyValueRecord> PatchAsync(string key, JsonNode? payload, long? expectedVersion, CancellationToken cancellationToken);
}

