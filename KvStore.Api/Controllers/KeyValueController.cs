using System.Text.Json.Nodes;
using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.KeyValue.Commands.PatchKeyValue;
using KvStore.Core.Application.KeyValue.Commands.PutKeyValue;
using KvStore.Core.Application.KeyValue.Queries.GetKeyValue;
using KvStore.Core.Application.KeyValue.Responses;
using Microsoft.AspNetCore.Mvc;

namespace KvStore.Api.Controllers;

[ApiController]
[Route("kv")]
public sealed class KeyValueController(
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher) : ControllerBase
{
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<KeyValueResponse>> GetAsync(string key, CancellationToken cancellationToken)
    {
        var response = await queryDispatcher.DispatchAsync(new GetKeyValueQuery(key), cancellationToken);
        return Ok(response);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KeyValueResponse>> PutAsync(
        string key,
        [FromBody] JsonNode? value,
        [FromQuery(Name = "ifVersion")] long? expectedVersion,
        CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.DispatchAsync(
            new PutKeyValueCommand(key, value, expectedVersion),
            cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KeyValueResponse>> PatchAsync(
        string key,
        [FromBody] JsonNode? delta,
        [FromQuery(Name = "ifVersion")] long? expectedVersion,
        CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.DispatchAsync(
            new PatchKeyValueCommand(key, delta, expectedVersion),
            cancellationToken);
        return Ok(response);
    }
}

