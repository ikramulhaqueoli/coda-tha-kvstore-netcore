using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using KvStore.Router.Clients;
using KvStore.Router.Models;
using KvStore.Router.Services;
using Microsoft.AspNetCore.Mvc;

namespace KvStore.Router.Controllers;

[ApiController]
[Route("kv")]
public sealed class KeyValueController(
    IKeyValueForwardingService forwardingService,
    IKeyListingService listingService,
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
            return CreateNodeErrorResult(ex);
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
        CancellationToken cancellationToken,
        [FromQuery] bool debug = false)
    {
        try
        {
            var result = await forwardingService.GetAsync(key, cancellationToken);
            return Ok(CreateResponse(result, debug));
        }
        catch (NodeHttpException ex)
        {
            return CreateNodeErrorResult(ex);
        }
        catch (NodeUnavailableException ex)
        {
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
        [FromQuery(Name = "ifVersion")] long? expectedVersion,
        CancellationToken cancellationToken,
        [FromQuery] bool debug = false)
    {
        try
        {
            var result = await forwardingService.PutAsync(key, value, expectedVersion, cancellationToken);
            return Ok(CreateResponse(result, debug));
        }
        catch (NodeHttpException ex)
        {
            return CreateNodeErrorResult(ex);
        }
        catch (NodeUnavailableException ex)
        {
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
        [FromQuery(Name = "ifVersion")] long? expectedVersion,
        CancellationToken cancellationToken,
        [FromQuery] bool debug = false)
    {
        try
        {
            var result = await forwardingService.PatchAsync(key, delta, expectedVersion, cancellationToken);
            return Ok(CreateResponse(result, debug));
        }
        catch (NodeHttpException ex)
        {
            return CreateNodeErrorResult(ex);
        }
        catch (NodeUnavailableException ex)
        {
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

    private ActionResult CreateNodeErrorResult(NodeHttpException exception)
    {
        logger.LogWarning(
            "Node {NodeId} responded with status {StatusCode}",
            exception.Node.Id,
            (int)exception.StatusCode);

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
        logger.LogError(exception, "Node {NodeId} is unavailable.", exception.Node.Id);
        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new { error = $"Node '{exception.Node.Id}' is unavailable." });
    }

    private static object CreateResponse(ForwardedKeyValueResult result, bool debug)
    {
        if (!debug)
        {
            return result.Record;
        }

        return new
        {
            result.Record.Key,
            result.Record.Value,
            result.Record.Version,
            debug = new { node = result.NodeId }
        };
    }
}

