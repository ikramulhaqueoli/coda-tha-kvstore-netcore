using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using KvStore.Router.Models;
using KvStore.Router.Nodes;

namespace KvStore.Router.Clients;

public sealed class KvStoreNodeClient(IHttpClientFactory httpClientFactory) : IKvStoreNodeClient
{
    public const string HttpClientName = "kvstore-node";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<KeyValueRecord> GetAsync(NodeDefinition node, string key, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildKeyUri(node, key));
        using var response = await SendAsync(node, request, cancellationToken);
        await EnsureSuccessAsync(node, response);
        return await ReadRecordAsync(response, cancellationToken);
    }

    public async Task<KeyValueRecord> PutAsync(
        NodeDefinition node,
        string key,
        JsonNode? payload,
        long? expectedVersion,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, BuildKeyUri(node, key, expectedVersion))
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await SendAsync(node, request, cancellationToken);
        await EnsureSuccessAsync(node, response);
        return await ReadRecordAsync(response, cancellationToken);
    }

    public async Task<KeyValueRecord> PatchAsync(
        NodeDefinition node,
        string key,
        JsonNode? payload,
        long? expectedVersion,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, BuildKeyUri(node, key, expectedVersion))
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await SendAsync(node, request, cancellationToken);
        await EnsureSuccessAsync(node, response);
        return await ReadRecordAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> ListKeysAsync(NodeDefinition node, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildKeysUri(node));
        using var response = await SendAsync(node, request, cancellationToken);
        await EnsureSuccessAsync(node, response);

        var keys = await response.Content.ReadFromJsonAsync<string[]>(SerializerOptions, cancellationToken);
        return keys ?? Array.Empty<string>();
    }

    private static Uri BuildKeyUri(NodeDefinition node, string key, long? expectedVersion = null)
    {
        var builder = new UriBuilder(node.BaseAddress)
        {
            Path = $"kv/{Uri.EscapeDataString(key)}",
            Query = expectedVersion.HasValue ? $"ifVersion={expectedVersion.Value}" : string.Empty
        };
        return new Uri(builder.ToString().Replace("http://", "https://"));
    }

    private static Uri BuildKeysUri(NodeDefinition node)
    {
        var builder = new UriBuilder(node.BaseAddress)
        {
            Path = "kv"
        };
        return builder.Uri;
    }

    private async Task<HttpResponseMessage> SendAsync(
        NodeDefinition node,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new NodeUnavailableException(node, ex);
        }
    }

    private static async Task EnsureSuccessAsync(NodeDefinition node, HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new NodeHttpException(node, response.StatusCode, body);
    }

    private static async Task<KeyValueRecord> ReadRecordAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var record = await response.Content.ReadFromJsonAsync<KeyValueRecord>(SerializerOptions, cancellationToken);
        return record ?? throw new InvalidOperationException("The downstream response did not contain a body.");
    }
}

