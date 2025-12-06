using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using KvStore.Router.Clients;
using KvStore.Router.Controllers;
using KvStore.Router.Models;
using KvStore.Router.Nodes;
using KvStore.Router.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using KvStore.UnitTest.TestHelpers;

namespace KvStore.UnitTest.Router.Controllers;

public sealed class KeyValueControllerTests
{
    [Fact]
    public async Task GetAsync_ReturnsOk_WhenSuccessful()
    {
        var forwardingService = new TestForwardingService
        {
            GetResult = new ForwardedKeyValueResult(
                new KeyValueRecord("test", JsonValue.Create(42), 1),
                "node-1",
                TimeSpan.FromMilliseconds(10))
        };
        var listingService = new TestListingService();
        var logger = new TestLogger<KeyValueController>();
        var controller = new KeyValueController(forwardingService, listingService, logger);

        var result = await controller.GetAsync("test", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var record = Assert.IsType<KeyValueRecord>(okResult.Value);
        Assert.Equal("test", record.Key);
        Assert.Equal(42, record.Value?.GetValue<int>());
    }

    [Fact]
    public async Task GetAsync_WithDebug_ReturnsDebugInfo()
    {
        var forwardingService = new TestForwardingService
        {
            GetResult = new ForwardedKeyValueResult(
                new KeyValueRecord("test", JsonValue.Create(42), 1),
                "node-1",
                TimeSpan.FromMilliseconds(50))
        };
        var listingService = new TestListingService();
        var logger = new TestLogger<KeyValueController>();
        var controller = new KeyValueController(forwardingService, listingService, logger);

        var result = await controller.GetAsync("test", CancellationToken.None, debug: true);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("node-1", json);
        Assert.Contains("executionTimeMs", json);
    }

    [Fact]
    public async Task GetAsync_ReturnsNodeError_WhenNodeHttpException()
    {
        var forwardingService = new TestForwardingService
        {
            GetException = new NodeHttpException(
                new NodeDefinition("node-1", new Uri("http://node-1/")),
                System.Net.HttpStatusCode.NotFound,
                "Key not found")
        };
        var listingService = new TestListingService();
        var logger = new TestLogger<KeyValueController>();
        var controller = new KeyValueController(forwardingService, listingService, logger);

        var result = await controller.GetAsync("test", CancellationToken.None);

        var statusCodeResult = Assert.IsType<ContentResult>(result.Result);
        Assert.Equal(404, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetAsync_ReturnsServiceUnavailable_WhenNodeUnavailable()
    {
        var forwardingService = new TestForwardingService
        {
            GetException = new NodeUnavailableException(
                new NodeDefinition("node-1", new Uri("http://node-1/")),
                new Exception("Connection failed"))
        };
        var listingService = new TestListingService();
        var logger = new TestLogger<KeyValueController>();
        var controller = new KeyValueController(forwardingService, listingService, logger);

        var result = await controller.GetAsync("test", CancellationToken.None);

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task PutAsync_ReturnsOk_WhenSuccessful()
    {
        var forwardingService = new TestForwardingService
        {
            PutResult = new ForwardedKeyValueResult(
                new KeyValueRecord("test", JsonValue.Create(42), 1),
                "node-1",
                TimeSpan.Zero)
        };
        var listingService = new TestListingService();
        var logger = new TestLogger<KeyValueController>();
        var controller = new KeyValueController(forwardingService, listingService, logger);

        var result = await controller.PutAsync("test", JsonValue.Create(42), expectedVersion: null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PatchAsync_ReturnsOk_WhenSuccessful()
    {
        var forwardingService = new TestForwardingService
        {
            PatchResult = new ForwardedKeyValueResult(
                new KeyValueRecord("test", JsonValue.Create(42), 2),
                "node-1",
                TimeSpan.Zero)
        };
        var listingService = new TestListingService();
        var logger = new TestLogger<KeyValueController>();
        var controller = new KeyValueController(forwardingService, listingService, logger);

        var result = await controller.PatchAsync("test", JsonNode.Parse("""{"count":1}"""), expectedVersion: null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ListAsync_ReturnsNdjson()
    {
        var forwardingService = new TestForwardingService();
        var listingService = new TestListingService
        {
            ListResult = new[]
            {
                new KeyListingRecord("key1", "node-0"),
                new KeyListingRecord("key2", "node-1")
            }
        };
        var logger = new TestLogger<KeyValueController>();
        var controller = new KeyValueController(forwardingService, listingService, logger);

        var result = await controller.ListAsync(CancellationToken.None);

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.NotNull(contentResult.ContentType);
        Assert.Contains("application/x-ndjson", contentResult.ContentType);
        Assert.Contains("key1", contentResult.Content);
        Assert.Contains("key2", contentResult.Content);
    }

    [Fact]
    public async Task ListAsync_ReturnsNodeError_WhenNodeHttpException()
    {
        var forwardingService = new TestForwardingService();
        var listingService = new TestListingService
        {
            ListException = new NodeHttpException(
                new NodeDefinition("node-1", new Uri("http://node-1/")),
                System.Net.HttpStatusCode.InternalServerError,
                "Internal error")
        };
        var logger = new TestLogger<KeyValueController>();
        var controller = new KeyValueController(forwardingService, listingService, logger);

        var result = await controller.ListAsync(CancellationToken.None);

        var statusCodeResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    private sealed class TestForwardingService : IKeyValueForwardingService
    {
        public ForwardedKeyValueResult? GetResult { get; set; }
        public Exception? GetException { get; set; }
        public ForwardedKeyValueResult? PutResult { get; set; }
        public Exception? PutException { get; set; }
        public ForwardedKeyValueResult? PatchResult { get; set; }
        public Exception? PatchException { get; set; }

        public Task<ForwardedKeyValueResult> GetAsync(string key, CancellationToken cancellationToken)
        {
            if (GetException != null) throw GetException;
            return Task.FromResult(GetResult!);
        }

        public Task<ForwardedKeyValueResult> PutAsync(
            string key,
            JsonNode? payload,
            int? expectedVersion,
            CancellationToken cancellationToken)
        {
            if (PutException != null) throw PutException;
            return Task.FromResult(PutResult!);
        }

        public Task<ForwardedKeyValueResult> PatchAsync(
            string key,
            JsonNode? payload,
            int? expectedVersion,
            CancellationToken cancellationToken)
        {
            if (PatchException != null) throw PatchException;
            return Task.FromResult(PatchResult!);
        }
    }

    private sealed class TestListingService : IKeyListingService
    {
        public IReadOnlyList<KeyListingRecord>? ListResult { get; set; } = Array.Empty<KeyListingRecord>();
        public Exception? ListException { get; set; }

        public Task<IReadOnlyList<KeyListingRecord>> ListAsync(CancellationToken cancellationToken)
        {
            if (ListException != null) throw ListException;
            return Task.FromResult(ListResult!);
        }
    }

}

