using System.Text.Json;
using KvStore.Api.Middleware;
using KvStore.Core.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using KvStore.UnitTest.TestHelpers;

namespace KvStore.UnitTest.Api.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_HandlesInvalidKeyException()
    {
        var middleware = CreateMiddleware(context =>
        {
            throw new InvalidKeyException("invalid key");
        });

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
        Assert.NotNull(context.Response.ContentType);
        Assert.Contains("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_HandlesInvalidPatchDeltaException()
    {
        var middleware = CreateMiddleware(context =>
        {
            throw new InvalidPatchDeltaException();
        });

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_HandlesVersionMismatchException()
    {
        var middleware = CreateMiddleware(context =>
        {
            throw new VersionMismatchException("key", 5, 1);
        });

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(409, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_HandlesKeyValueNotFoundException()
    {
        var middleware = CreateMiddleware(context =>
        {
            throw new KeyValueNotFoundException("missing-key");
        });

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_HandlesJsonException()
    {
        var middleware = CreateMiddleware(context =>
        {
            throw new JsonException("Invalid JSON");
        });

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_HandlesOperationCanceledException_WhenRequestAborted()
    {
        var middleware = CreateMiddleware(context =>
        {
            context.RequestAborted.ThrowIfCancellationRequested();
            throw new OperationCanceledException();
        });

        var context = CreateHttpContext();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        context.RequestAborted = cts.Token;

        await middleware.InvokeAsync(context);

        Assert.Equal(499, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_HandlesGenericException()
    {
        var middleware = CreateMiddleware(context =>
        {
            throw new InvalidOperationException("Unexpected error");
        });

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotWrite_WhenResponseHasStarted()
    {
        var middleware = CreateMiddleware(context =>
        {
            throw new InvalidKeyException("test");
        });

        var context = CreateHttpContext();
        // Start the response by writing headers
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("started");

        await middleware.InvokeAsync(context);

        // Response has started, middleware should not modify it
        Assert.True(context.Response.HasStarted);
    }

    [Fact]
    public async Task InvokeAsync_ProceedsNormally_WhenNoException()
    {
        var called = false;
        var middleware = CreateMiddleware(context =>
        {
            called = true;
            return Task.CompletedTask;
        });

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Equal(200, context.Response.StatusCode);
    }

    private static ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
    {
        var logger = new TestLogger<ExceptionHandlingMiddleware>();
        return new ExceptionHandlingMiddleware(next, logger);
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

}

