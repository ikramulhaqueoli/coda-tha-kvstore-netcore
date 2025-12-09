using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using KvStore.Router.Clients;
using KvStore.Router.Models;
using KvStore.Router.Services;
using Microsoft.AspNetCore.Mvc;

namespace KvStore.Router.Controllers;

[ApiController]
[Route("router/kv")]
public sealed class KeyValueController(
    IKeyValueForwardingService forwardingService,
    IKeyListingService listingService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<KeyValueController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions NdjsonSerializerOptions = new()
    {
        PropertyNamingPolicy = null
    };

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        try
        {
            var records = await listingService.ListAsync(cancellationToken);
            var payload = ToNdjson(records);
            return Content(payload, "application/x-ndjson", Encoding.UTF8);
        }
        catch (NodeHttpException ex)
        {
            return CreateNodeErrorResult(ex, new TimeSpan());
        }
        catch (NodeUnavailableException ex)
        {
            return CreateNodeUnavailableResult(ex);
        }
    }

    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KeyValueRecord>> GetAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await forwardingService.GetAsync(key, cancellationToken);
            stopwatch.Stop();
            return CreateResponse(result);
        }
        catch (NodeHttpException ex)
        {
            stopwatch.Stop();
            return CreateNodeErrorResult(ex, stopwatch.Elapsed);
        }
        catch (NodeUnavailableException ex)
        {
            stopwatch.Stop();
            return CreateNodeUnavailableResult(ex);
        }
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KeyValueRecord>> PutAsync(
        string key,
        [FromBody] JsonNode? value,
        [FromQuery(Name = "ifVersion")] int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await forwardingService.PutAsync(key, value, expectedVersion, cancellationToken);
            stopwatch.Stop();
            return CreateResponse(result);
        }
        catch (NodeHttpException ex)
        {
            stopwatch.Stop();
            return CreateNodeErrorResult(ex, stopwatch.Elapsed);
        }
        catch (NodeUnavailableException ex)
        {
            stopwatch.Stop();
            return CreateNodeUnavailableResult(ex);
        }
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KeyValueRecord>> PatchAsync(
        string key,
        [FromBody] JsonNode? delta,
        [FromQuery(Name = "ifVersion")] int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await forwardingService.PatchAsync(key, delta, expectedVersion, cancellationToken);
            stopwatch.Stop();
            return CreateResponse(result);
        }
        catch (NodeHttpException ex)
        {
            stopwatch.Stop();
            return CreateNodeErrorResult(ex, stopwatch.Elapsed);
        }
        catch (NodeUnavailableException ex)
        {
            stopwatch.Stop();
            return CreateNodeUnavailableResult(ex);
        }
    }

    private static string ToNdjson(IEnumerable<KeyListingRecord> records)
    {
        var builder = new StringBuilder();
        foreach (var record in records)
        {
            var line = JsonSerializer.Serialize(record, NdjsonSerializerOptions);
            builder.AppendLine(line);
        }

        return builder.ToString();
    }

    private ActionResult CreateNodeErrorResult(NodeHttpException exception, TimeSpan executionTime)
    {
        var requestHash = httpContextAccessor.HttpContext?.Request.GetHashCode() ?? 0;
        
        logger.LogWarning(
            "Request #{RequestHash}: Node {NodeId} responded with status {StatusCode}",
            requestHash,
            exception.Node.Id,
            (int)exception.StatusCode);

        // Always add debug information to response headers
        Response.Headers["X-Debug-RequestHash"] = requestHash.ToString();
        Response.Headers["X-Debug-Node"] = exception.Node.Id;
        Response.Headers["X-Debug-ExecutionTimeMs"] = executionTime.TotalMilliseconds.ToString("F2");

        if (string.IsNullOrWhiteSpace(exception.ResponseBody))
        {
            return StatusCode((int)exception.StatusCode);
        }

        return new ContentResult
        {
            StatusCode = (int)exception.StatusCode,
            ContentType = "application/json",
            Content = exception.ResponseBody
        };
    }

    private ActionResult CreateNodeUnavailableResult(NodeUnavailableException exception)
    {
        var requestHash = httpContextAccessor.HttpContext?.Request.GetHashCode() ?? 0;
        
        logger.LogError(exception, "Request #{RequestHash}: Node {NodeId} is unavailable.", requestHash, exception.Node.Id);
        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new { error = $"Node '{exception.Node.Id}' is unavailable." });
    }

    private ActionResult<KeyValueRecord> CreateResponse(ForwardedKeyValueResult result)
    {
        var requestHash = httpContextAccessor.HttpContext?.Request.GetHashCode() ?? 0;
        
        Response.Headers["X-Debug-RequestHash"] = requestHash.ToString();
        Response.Headers["X-Debug-Node"] = result.NodeId;
        Response.Headers["X-Debug-ExecutionTimeMs"] = result.ExecutionTime.TotalMilliseconds.ToString("F2");

        return Ok(result.Record);
    }
}

