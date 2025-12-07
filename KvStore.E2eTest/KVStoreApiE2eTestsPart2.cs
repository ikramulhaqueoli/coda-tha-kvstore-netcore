using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace KvStore.E2eTest;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class RemoteKvStoreRouterCollection : ICollectionFixture<RemoteKvStoreRouterFixture>
{
    public const string CollectionName = "RemoteKvStoreRouterApi";
}

public sealed class RemoteKvStoreRouterFixture : IDisposable
{
    public RemoteKvStoreRouterFixture()
    {
        Client = new HttpClient
        {
            BaseAddress = new Uri("http://136.110.4.225:7001/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
    }
}

[Collection(RemoteKvStoreRouterCollection.CollectionName)]
public sealed class KVStoreApiE2eTestsPart2
{
    private readonly HttpClient _client;

    public KVStoreApiE2eTestsPart2(RemoteKvStoreRouterFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact(DisplayName = "Case: Conditional PUT through router enforces optimistic locking.")]
    public async Task Put_ConditionalGuardHonorsOptimisticLock()
    {
        var key = CreateKey();

        using var initialResponse = await PutAsync(key, JsonValue.Create(1));
        var created = await AssertSuccessAsync(initialResponse);

        using var mismatchResponse = await PutAsync(key, JsonValue.Create(2), ifVersion: created.Version + 5);
        Assert.Equal(HttpStatusCode.Conflict, mismatchResponse.StatusCode);

        using var guardedResponse = await PutAsync(key, JsonValue.Create(3), ifVersion: created.Version);
        var updated = await AssertSuccessAsync(guardedResponse);

        Assert.Equal(created.Version + 1, updated.Version);
        Assert.Equal(3, updated.Value!.GetValue<int>());
    }

    [Fact(DisplayName = "Case: 20 concurrent mixed requests (GET, PUT, PATCH) with same key return 200.")]
    public async Task Mixed20ConcurrentRequests_SameKey_Returns200()
    {
        var key = CreateKey();

        var tasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(async () =>
            {
                var operation = i % 3;
                HttpResponseMessage response = operation switch
                {
                    0 => await PutAsync(key, JsonValue.Create(i)),
                    1 => await GetAsync(key),
                    2 => await PatchAsync(key, ParseJson($"{{\"counter\":{i}}}")),
                    _ => throw new InvalidOperationException(),
                };
                using (response)
                {
                    return response.StatusCode;
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        Assert.All(results, statusCode => Assert.Equal(HttpStatusCode.OK, statusCode));
    }

    [Fact(DisplayName = "Case: 20 concurrent mixed requests (GET, PUT, PATCH) with distinct keys return 200.")]
    public async Task Mixed20ConcurrentRequests_DistinctKeys_Returns200()
    {
        var keys = Enumerable.Range(0, 20)
            .Select(i => CreateKey(suffix: i.ToString()))
            .ToList();

        var tasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(async () =>
            {
                var key = keys[i];
                var operation = i % 3;
                HttpResponseMessage response = operation switch
                {
                    0 => await PutAsync(key, JsonValue.Create(i)),
                    1 => await GetAsync(key),
                    2 => await PatchAsync(key, ParseJson($"{{\"counter\":{i}}}")),
                    _ => throw new InvalidOperationException(),
                };
                using (response)
                {
                    return response.StatusCode;
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        Assert.All(results, statusCode => Assert.Equal(HttpStatusCode.OK, statusCode));
    }

    [Fact(DisplayName = "Case: PATCH merge via router keeps existing fields and bumps version.")]
    public async Task Patch_ShallowMergePreservesExistingFields()
    {
        var key = CreateKey();
        var baseline = ParseJson("""{"name":"Ari","points":10}""");
        var delta = ParseJson("""{"rank":"gold"}""");

        using var putResponse = await PutAsync(key, baseline);
        await AssertSuccessAsync(putResponse);

        using var patchResponse = await PatchAsync(key, delta);
        var patched = await AssertSuccessAsync(patchResponse);

        var expected = ParseJson("""{"name":"Ari","points":10,"rank":"gold"}""");
        AssertJsonEqual(expected, patched.Value);
        Assert.Equal(2, patched.Version);
    }

    [Fact(DisplayName = "Case: Router key listing (NDJSON) contains created keys with node metadata.")]
    public async Task ListKeys_ReturnsNdjsonWithNodeMetadata()
    {
        var keyA = CreateKey("list-A");
        var keyB = CreateKey("list-B");

        using var putA = await PutAsync(keyA, JsonValue.Create("alpha"));
        using var putB = await PutAsync(keyB, JsonValue.Create("beta"));
        await AssertSuccessAsync(putA);
        await AssertSuccessAsync(putB);

        var listings = await ListKeysAsync();

        var entryA = listings.Single(record => record.Key == keyA);
        var entryB = listings.Single(record => record.Key == keyB);

        Assert.False(string.IsNullOrWhiteSpace(entryA.Node));
        Assert.False(string.IsNullOrWhiteSpace(entryB.Node));
    }

    [Fact(DisplayName = "Case: 3 concurrent router clients increment counters 100x and reach expected totals.")]
    public async Task ConcurrentClientsThroughRouterMaintainPerKeyAtomicity()
    {
        var key = CreateKey("router-counter");

        using var createResponse = await PutAsync(key, JsonValue.Create(0));
        await AssertSuccessAsync(createResponse);

        const int clients = 3;
        const int incrementsPerClient = 100;

        var tasks = Enumerable
            .Range(0, clients)
            .Select(_ => Task.Run(() => IncrementCounterAsync(key, incrementsPerClient)))
            .ToArray();

        await Task.WhenAll(tasks);

        using var finalResponse = await GetAsync(key);
        var payload = await AssertSuccessAsync(finalResponse);

        Assert.Equal(clients * incrementsPerClient, payload.Value!.GetValue<int>());
        Assert.Equal(1 + clients * incrementsPerClient, payload.Version);
    }

    private async Task IncrementCounterAsync(string key, int increments)
    {
        for (var i = 0; i < increments; i++)
        {
            while (true)
            {
                using var getResponse = await GetAsync(key);
                var current = await AssertSuccessAsync(getResponse);
                var nextValue = JsonValue.Create(current.Value!.GetValue<int>() + 1);

                using var putResponse = await PutAsync(key, nextValue, current.Version);
                if (putResponse.StatusCode == HttpStatusCode.OK)
                {
                    break;
                }

                Assert.Equal(HttpStatusCode.Conflict, putResponse.StatusCode);
            }
        }
    }

    private Task<HttpResponseMessage> PutAsync(string key, JsonNode? value, int? ifVersion = null, bool debug = false)
        => SendAsync(HttpMethod.Put, key, value?.ToJsonString() ?? "null", ifVersion, debug, "application/json");

    private Task<HttpResponseMessage> PatchAsync(string key, JsonNode? delta, int? ifVersion = null)
        => SendAsync(HttpMethod.Patch, key, delta?.ToJsonString() ?? "null", ifVersion, debug: false, "application/json");

    private Task<HttpResponseMessage> GetAsync(string key)
    {
        var uri = $"router/kv/{Uri.EscapeDataString(key)}";
        return _client.GetAsync(uri);
    }

    private async Task<IReadOnlyList<KeyListingRecord>> ListKeysAsync()
    {
        using var response = await _client.GetAsync("router/kv");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        var lines = payload
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var records = new List<KeyListingRecord>();
        foreach (var line in lines)
        {
            var node = JsonNode.Parse(line)!.AsObject();
            records.Add(new KeyListingRecord(
                node["Key"]!.GetValue<string>(),
                node["Node"]!.GetValue<string>()));
        }

        return records;
    }

    private Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string key,
        string body,
        int? ifVersion,
        bool debug,
        string? contentType)
    {
        var requestUri = BuildUri(key, ifVersion, debug);
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = CreateContent(body, contentType)
        };

        return _client.SendAsync(request);
    }

    private static HttpContent CreateContent(string body, string? contentType)
    {
        var content = new StringContent(body, Encoding.UTF8);
        if (contentType != null)
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }
        else
        {
            content.Headers.ContentType = null;
        }

        return content;
    }

    private static string BuildUri(string key, int? ifVersion, bool debug)
    {
        var encodedKey = Uri.EscapeDataString(key);
        var path = $"router/kv/{encodedKey}";
        var query = new List<string>();

        if (ifVersion.HasValue)
        {
            query.Add($"ifVersion={ifVersion.Value}");
        }

        if (debug)
        {
            query.Add("debug=true");
        }

        return query.Count > 0 ? $"{path}?{string.Join('&', query)}" : path;
    }

    private static string CreateKey(string? suffix = null)
        => $"{Stopwatch.GetTimestamp()}{(suffix != null ? ":" + suffix : "")}";

    private static JsonNode ParseJson(string value)
        => JsonNode.Parse(value)!;

    private static async Task<KeyValueRecord> AssertSuccessAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await DeserializeAsync(response);
    }

    private static async Task<KeyValueRecord> DeserializeAsync(HttpResponseMessage response)
    {
        var serialized = await response.Content.ReadAsStringAsync();
        var obj = JsonNode.Parse(serialized)!.AsObject();

        string? nodeId = null;
        if (obj.TryGetPropertyValue("debug", out var debugNode) && debugNode is JsonObject debugObj)
        {
            nodeId = debugObj["node"]?.GetValue<string>();
        }

        return new KeyValueRecord(
            obj["Key"]!.GetValue<string>(),
            obj["Value"],
            obj["Version"]!.GetValue<int>(),
            nodeId);
    }

    private static void AssertJsonEqual(JsonNode? expected, JsonNode? actual)
    {
        Assert.True(
            JsonNode.DeepEquals(expected, actual),
            $"Expected {expected?.ToJsonString()} but got {actual?.ToJsonString()}");
    }

    private sealed record KeyValueRecord(string Key, JsonNode? Value, int Version, string? NodeId);

    private sealed record KeyListingRecord(string Key, string Node);
}

