using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace KvStore.E2eTest;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class RemoteKvStoreApiCollection : ICollectionFixture<RemoteKvStoreClientFixture>
{
    public const string CollectionName = "RemoteKvStoreApi";
}

public sealed class RemoteKvStoreClientFixture : IDisposable
{
    public RemoteKvStoreClientFixture()
    {
        Client = new HttpClient
        {
            BaseAddress = new Uri("http://34.87.122.195:7000/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
    }
}

[Collection(RemoteKvStoreApiCollection.CollectionName)]
public sealed class KVStoreApiE2eTestsPart1
{
    private readonly HttpClient _client;

    public KVStoreApiE2eTestsPart1(RemoteKvStoreClientFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact(DisplayName = "Case: Concurrent 3 clients increment counters 100 times and count reaches 300.")]
    public async Task Concurrent3ClientsIncrementCounters100TimesAndGetsFinalCount300()
    {
        var key = CreateKey();
        using var createResponse = await PutAsync(key, JsonValue.Create(0));
        await AssertSuccessAsync(createResponse);

        const int clients = 3;
        const int incrementsPerClient = 100;

        var tasks = Enumerable.Range(0, clients)
            .Select(_ => Task.Run(() => IncrementCounterAsync(key, incrementsPerClient)))
            .ToArray();

        await Task.WhenAll(tasks);

        using var finalResponse = await GetAsync(key);
        var finalPayload = await AssertSuccessAsync(finalResponse);

        Assert.Equal(clients * incrementsPerClient, finalPayload.Value!.GetValue<int>());
        Assert.Equal(1 + clients * incrementsPerClient, finalPayload.Version);
    }

    [Fact(DisplayName = "Case: GET retrieves an existing key with stored payload.")]
    public async Task Get_ExistingKey_ReturnsStoredValue()
    {
        var key = CreateKey();
        var value = ParseJson("""{"name":"Ari","points":10}""");

        using var putResponse = await PutAsync(key, value);
        await AssertSuccessAsync(putResponse);

        using var getResponse = await GetAsync(key);
        var payload = await AssertSuccessAsync(getResponse);

        Assert.Equal(key, payload.Key);
        AssertJsonEqual(value, payload.Value);
    }

    [Fact(DisplayName = "Case: Plain PUT creates a brand-new key with version 1.")]
    public async Task Put_CreatesNewKey_ReturnsVersionOne()
    {
        var key = CreateKey();
        var body = ParseJson("""{"name":"Ari","points":10}""");

        using var response = await PutAsync(key, body);
        var payload = await AssertSuccessAsync(response);

        Assert.Equal(key, payload.Key);
        AssertJsonEqual(body, payload.Value);
        Assert.Equal(1, payload.Version);
    }

    [Fact(DisplayName = "Case: PATCH upserts when key absent with version starting at 1.")]
    public async Task Patch_CreateNewKeyWhenAbsent()
    {
        var key = CreateKey();
        var delta = ParseJson("""{"rank":"gold"}""");

        using var response = await PatchAsync(key, delta);
        var payload = await AssertSuccessAsync(response);

        Assert.Equal(1, payload.Version);
        AssertJsonEqual(delta, payload.Value);
    }

    [Fact(DisplayName = "Case: 100 concurrent PUT requests with same key.")]
    public async Task Concurrent100Requests_SameKey()
    {
        var key = CreateKey();

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(async () =>
            {
                using var response = await PutAsync(key, JsonValue.Create(0));
                return response.StatusCode;
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        Assert.All(results, statusCode => Assert.Equal(HttpStatusCode.OK, statusCode));
    }

    [Fact(DisplayName = "Case: 100 concurrent PUT requests with distinct keys.")]
    public async Task Concurrent100Requests_DistinctKeys()
    {
        var keys = Enumerable.Range(0, 100)
            .Select(i => CreateKey(suffix: i.ToString()))
            .ToList();

        var tasks = keys.Select(key => Task.Run(async () =>
        {
            using var response = await PutAsync(key, JsonValue.Create(0));
            return response.StatusCode;
        }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        Assert.All(results, statusCode => Assert.Equal(HttpStatusCode.OK, statusCode));
    }

    [Fact(DisplayName = "Case: GET against unknown key returns 404.")]
    public async Task Get_MissingKey_ReturnsNotFound()
    {
        using var response = await GetAsync($"missing-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "Case: Conditional PUT succeeds when ifVersion matches current version.")]
    public async Task Put_ConditionalSuccess_UsesOptimisticLock()
    {
        var key = CreateKey();

        using var initialResponse = await PutAsync(key, JsonValue.Create(1));
        var initialPayload = await AssertSuccessAsync(initialResponse);

        using var conditionalResponse = await PutAsync(key, JsonValue.Create(2), initialPayload.Version);
        var updatedPayload = await AssertSuccessAsync(conditionalResponse);

        Assert.Equal(2, updatedPayload.Value!.GetValue<int>());
        Assert.Equal(initialPayload.Version + 1, updatedPayload.Version);
    }

    [Fact(DisplayName = "Case: Conditional PUT fails with 409 when version mismatches.")]
    public async Task Put_ConditionalMismatch_ReturnsConflictAndPreservesVersion()
    {
        var key = CreateKey();
        using var initialResponse = await PutAsync(key, JsonValue.Create(1));
        var initialPayload = await AssertSuccessAsync(initialResponse);

        using var conflictResponse = await PutAsync(key, JsonValue.Create(3), ifVersion: initialPayload.Version + 1);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

        using var getResponse = await GetAsync(key);
        var current = await AssertSuccessAsync(getResponse);

        Assert.Equal(initialPayload.Version, current.Version);
        Assert.Equal(1, current.Value!.GetValue<int>());
    }

    [Fact(DisplayName = "Case: PATCH merges JSON objects shallowly, preserving prior fields.")]
    public async Task Patch_MergesObjectsShallowly()
    {
        var key = CreateKey();
        var original = ParseJson("""{"name":"Ari","points":10}""");
        var delta = ParseJson("""{"rank":"gold"}""");

        using var putResponse = await PutAsync(key, original);
        await AssertSuccessAsync(putResponse);

        using var patchResponse = await PatchAsync(key, delta);
        var payload = await AssertSuccessAsync(patchResponse);

        var expected = ParseJson("""{"name":"Ari","points":10,"rank":"gold"}""");
        AssertJsonEqual(expected, payload.Value);
        Assert.Equal(2, payload.Version);
    }

    [Fact(DisplayName = "Case: Primitive delta against object value replaces entire value.")]
    public async Task Patch_WithPrimitiveDeltaReplacesExistingObject()
    {
        var key = CreateKey();
        var original = ParseJson("""{"name":"Ari"}""");

        using var putResponse = await PutAsync(key, original);
        await AssertSuccessAsync(putResponse);

        using var patchResponse = await PatchAsync(key, JsonValue.Create(123));
        var payload = await AssertSuccessAsync(patchResponse);

        Assert.Equal(123, payload.Value!.GetValue<int>());
        Assert.Equal(2, payload.Version);
    }

    [Fact(DisplayName = "Case: Conditional PATCH with wrong version yields 409 and no change.")]
    public async Task Patch_ConditionalMismatchReturnsConflict()
    {
        var key = CreateKey();
        using var putResponse = await PutAsync(key, ParseJson("""{"points":10}"""));
        var initial = await AssertSuccessAsync(putResponse);

        using var conflictResponse = await PatchAsync(key, ParseJson("""{"rank":"gold"}"""), initial.Version + 5);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

        using var getResponse = await GetAsync(key);
        var current = await AssertSuccessAsync(getResponse);
        Assert.Equal(initial.Version, current.Version);
        AssertJsonEqual(ParseJson("""{"points":10}"""), current.Value);
    }

    [Fact(DisplayName = "Case: Successful writes increment version exactly once; failed guards don't.")]
    public async Task VersionsIncrementExactlyOncePerSuccessfulWrite()
    {
        var key = CreateKey();

        using var createResponse = await PutAsync(key, ParseJson("""{"value":1}"""));
        var created = await AssertSuccessAsync(createResponse);
        Assert.Equal(1, created.Version);

        using var replaceResponse = await PutAsync(key, ParseJson("""{"value":2}"""));
        var replaced = await AssertSuccessAsync(replaceResponse);
        Assert.Equal(2, replaced.Version);

        using var patchResponse = await PatchAsync(key, ParseJson("""{"extra":true}"""));
        var patched = await AssertSuccessAsync(patchResponse);
        Assert.Equal(3, patched.Version);

        using var conflictResponse = await PutAsync(key, ParseJson("""{"value":99}"""), ifVersion: 1);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

        using var finalGet = await GetAsync(key);
        var finalState = await AssertSuccessAsync(finalGet);
        Assert.Equal(3, finalState.Version);
    }

    #region Additional coverage (optional)
    [Fact(DisplayName = "Case: PUT with malformed JSON body returns 400.")]
    public async Task Put_WithMalformedJson_ReturnsBadRequest()
    {
        var key = CreateKey();
        using var response = await PutRawAsync(key, "{ this is not json }");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Case: Conditional PATCH succeeds with matching version guard.")]
    public async Task Patch_ConditionalSuccessHonorsVersion()
    {
        var key = CreateKey();
        using var putResponse = await PutAsync(key, ParseJson("""{"points":10}"""));
        var initial = await AssertSuccessAsync(putResponse);

        using var patchResponse = await PatchAsync(key, ParseJson("""{"rank":"gold"}"""), initial.Version);
        var patched = await AssertSuccessAsync(patchResponse);

        Assert.Equal(initial.Version + 1, patched.Version);
        var expected = ParseJson("""{"points":10,"rank":"gold"}""");
        AssertJsonEqual(expected, patched.Value);
    }

    [Fact(DisplayName = "Case: Two guarded writes where only one should succeed; the other conflicts.")]
    public async Task ConcurrentGuardedWritesResultInSingleSuccess()
    {
        var key = CreateKey();
        using var initialResponse = await PutAsync(key, ParseJson("""{"value":"initial"}"""));
        var initial = await AssertSuccessAsync(initialResponse);

        var taskOne = Task.Run(async () =>
        {
            using var response = await PutAsync(key, ParseJson("""{"value":"first"}"""), initial.Version);
            return response.StatusCode;
        });

        var taskTwo = Task.Run(async () =>
        {
            using var response = await PutAsync(key, ParseJson("""{"value":"second"}"""), initial.Version);
            return response.StatusCode;
        });

        var results = await Task.WhenAll(taskOne, taskTwo);

        Assert.Contains(HttpStatusCode.OK, results);
        Assert.Contains(HttpStatusCode.Conflict, results);

        using var finalResponse = await GetAsync(key);
        var finalPayload = await AssertSuccessAsync(finalResponse);
        var finalValue = finalPayload.Value!["value"]!.GetValue<string>();
        Assert.True(finalValue is "first" or "second");
    }
    #endregion

    private async Task IncrementCounterAsync(string key, int increments)
    {
        for (var i = 0; i < increments; i++)
        {
            while (true)
            {
                using var getResponse = await GetAsync(key);
                var current = await AssertSuccessAsync(getResponse);
                var currentValue = current.Value!.GetValue<int>();
                var nextValue = JsonValue.Create(currentValue + 1);

                using var putResponse = await PutAsync(key, nextValue, current.Version);
                if (putResponse.StatusCode == HttpStatusCode.OK)
                {
                    break;
                }

                Assert.Equal(HttpStatusCode.Conflict, putResponse.StatusCode);
            }
        }
    }

    private Task<HttpResponseMessage> PutAsync(string key, JsonNode? value, int? ifVersion = null)
        => SendAsync(HttpMethod.Put, key, value?.ToJsonString() ?? "null", ifVersion, "application/json");

    private Task<HttpResponseMessage> PutRawAsync(string key, string body, int? ifVersion = null, string? contentType = "application/json")
        => SendAsync(HttpMethod.Put, key, body, ifVersion, contentType);

    private Task<HttpResponseMessage> PatchAsync(string key, JsonNode? delta, int? ifVersion = null)
        => SendAsync(HttpMethod.Patch, key, delta?.ToJsonString() ?? "null", ifVersion, "application/json");

    private Task<HttpResponseMessage> PatchRawAsync(string key, string body, int? ifVersion = null)
        => SendAsync(HttpMethod.Patch, key, body, ifVersion, "application/json");

    private Task<HttpResponseMessage> GetAsync(string key)
    {
        var uri = $"api/kv/{Uri.EscapeDataString(key)}";
        return _client.GetAsync(uri);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string key, string body, int? ifVersion, string? contentType)
    {
        var requestUri = BuildUri(key, ifVersion);
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

    private static string BuildUri(string key, int? ifVersion)
    {
        var encodedKey = Uri.EscapeDataString(key);
        var path = $"api/kv/{encodedKey}";
        return ifVersion.HasValue ? $"{path}?ifVersion={ifVersion.Value}" : path;
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

        return new KeyValueRecord(
            obj["Key"]!.GetValue<string>(),
            obj["Value"],
            obj["Version"]!.GetValue<int>());
    }

    private static void AssertJsonEqual(JsonNode? expected, JsonNode? actual)
    {
        Assert.True(JsonNode.DeepEquals(expected, actual), $"Expected {expected?.ToJsonString()} but got {actual?.ToJsonString()}");
    }

    private sealed record KeyValueRecord(string Key, JsonNode? Value, int Version);
}

