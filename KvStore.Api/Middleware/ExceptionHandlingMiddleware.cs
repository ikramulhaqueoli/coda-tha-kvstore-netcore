using System.Text.Json;
using KvStore.Core.Domain.Exceptions;

namespace KvStore.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (VersionMismatchException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (KeyValueNotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (JsonException)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "The request body was not valid JSON.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing request.");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}

