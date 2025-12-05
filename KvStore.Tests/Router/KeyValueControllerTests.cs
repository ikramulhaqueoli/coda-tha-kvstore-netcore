using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using KvStore.Router.Controllers;
using KvStore.Router.Models;
using KvStore.Router.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace KvStore.Tests.Router;

public sealed class KeyValueControllerTests
{
    [Fact]
    public async Task GetAsync_WithDebugTrue_IncludesExecutionTime()
    {
        var record = new KeyValueRecord("alpha", JsonValue.Create("beta"), 3);
        var forwardingService = new StubForwardingService(record, TimeSpan.FromMilliseconds(42));
        var controller = new KeyValueController(
            forwardingService,
            new StubListingService(),
            NullLogger<KeyValueController>.Instance);

        var actionResult = await controller.GetAsync("alpha", CancellationToken.None, true);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var serialized = JsonSerializer.Serialize(okResult.Value);

        using var document = JsonDocument.Parse(serialized);
        var debugElement = document.RootElement.GetProperty("debug");

        Assert.Equal("node-1", debugElement.GetProperty("node").GetString());
        Assert.Equal(42d, debugElement.GetProperty("executionTimeMs").GetDouble());
    }

    private sealed class StubForwardingService : IKeyValueForwardingService
    {
        private readonly ForwardedKeyValueResult result;

        public StubForwardingService(KeyValueRecord record, TimeSpan executionTime)
        {
            result = new ForwardedKeyValueResult(record, "node-1", executionTime);
        }

        public Task<ForwardedKeyValueResult> GetAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(result);

        public Task<ForwardedKeyValueResult> PutAsync(
            string key,
            JsonNode? payload,
            int? expectedVersion,
            CancellationToken cancellationToken) => Task.FromResult(result);

        public Task<ForwardedKeyValueResult> PatchAsync(
            string key,
            JsonNode? payload,
            int? expectedVersion,
            CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class StubListingService : IKeyListingService
    {
        public Task<IReadOnlyList<KeyListingRecord>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<KeyListingRecord>>(Array.Empty<KeyListingRecord>());
    }
}

