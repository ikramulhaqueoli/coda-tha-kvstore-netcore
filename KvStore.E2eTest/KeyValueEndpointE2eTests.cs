using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using KvStore.E2eTest.Fixtures;
using Xunit;

namespace KvStore.E2eTest;

public sealed class KeyValueEndpointE2eTests
{
    [Fact]
    public async Task Concurrent_Puts_On_Same_Key_Produce_Strict_Versions()
    {
        using var factory = new KvStoreApiFactory();
        using var client = factory.CreateClient();

        const string key = "sharedkey";
        const int requestCount = 500;

        var tasks = Enumerable.Range(1, requestCount)
            .Select(i => PutAsync(client, key, new { value = $"payload-{i}" }))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        var orderedVersions = responses
            .Select(r => (int)r.Version)
            .OrderBy(v => v)
            .ToArray();

        Assert.Equal(Enumerable.Range(1, requestCount), orderedVersions);

        var snapshot = await GetAsync(client, key);
        Assert.Equal(requestCount, snapshot.Version);
        Assert.NotNull(snapshot.Value);
    }

    [Fact]
    public async Task Concurrent_Puts_On_Different_Keys_All_Create_Version_One()
    {
        using var factory = new KvStoreApiFactory();
        using var client = factory.CreateClient();

        const int requestCount = 500;

        var tasks = Enumerable.Range(1, requestCount)
            .Select(i => PutAsync(client, $"key{i}", new { points = i }))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(1, r.Version));

        var snapshots = await Task.WhenAll(responses.Select(r => GetAsync(client, r.Key)));
        Assert.All(snapshots, snapshot => Assert.Equal(1, snapshot.Version));
    }

    [Fact]
    public async Task Happy_Path_For_All_Endpoints()
    {
        using var factory = new KvStoreApiFactory();
        using var client = factory.CreateClient();

        var key = $"happy{Guid.NewGuid():N}";

        var created = await PutAsync(client, key, new { name = "Ari", points = 10 }, expectedVersion: 0);
        Assert.Equal(1, created.Version);
        Assert.Equal("Ari", created.Value?["name"]?.GetValue<string>());
        Assert.Equal(10, created.Value?["points"]?.GetValue<int>());

        var fetched = await GetAsync(client, key);
        Assert.Equal(created.Version, fetched.Version);
        Assert.Equal("Ari", fetched.Value?["name"]?.GetValue<string>());

        var patched = await PatchAsync(client, key, new { points = 15, rank = "gold" }, expectedVersion: fetched.Version);
        Assert.Equal(fetched.Version + 1, patched.Version);
        Assert.Equal(15, patched.Value?["points"]?.GetValue<int>());
        Assert.Equal("gold", patched.Value?["rank"]?.GetValue<string>());

        var finalSnapshot = await GetAsync(client, key);
        Assert.Equal(patched.Version, finalSnapshot.Version);
        Assert.Equal("gold", finalSnapshot.Value?["rank"]?.GetValue<string>());
    }

    [Fact]
    public async Task Get_Missing_Key_Returns_NotFound()
    {
        using var factory = new KvStoreApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/kv/missing{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<KeyValueRecord> PutAsync(HttpClient client, string key, object payload, long? expectedVersion = null)
    {
        var url = $"/kv/{key}";
        if (expectedVersion.HasValue)
        {
            url += $"?ifVersion={expectedVersion.Value}";
        }

        var response = await client.PutAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KeyValueRecord>() ??
               throw new InvalidOperationException("Response did not contain a body.");
    }

    private static async Task<KeyValueRecord> GetAsync(HttpClient client, string key)
    {
        var response = await client.GetAsync($"/kv/{key}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KeyValueRecord>() ??
               throw new InvalidOperationException("Response did not contain a body.");
    }

    private static async Task<KeyValueRecord> PatchAsync(HttpClient client, string key, object payload, long? expectedVersion = null)
    {
        var url = $"/kv/{key}";
        if (expectedVersion.HasValue)
        {
            url += $"?ifVersion={expectedVersion.Value}";
        }

        var response = await client.PatchAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KeyValueRecord>() ??
               throw new InvalidOperationException("Response did not contain a body.");
    }

    private sealed record KeyValueRecord(string Key, JsonNode? Value, long Version);
}

