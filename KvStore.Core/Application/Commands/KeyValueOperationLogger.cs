using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace KvStore.Core.Application.Commands;

public static class KeyValueOperationLogger
{
    private static readonly ConcurrentDictionary<int, Stopwatch> _executionStopwatches = new();

    private static string GetActionMethod(Type? declaringType)
    {
        var upperDeclaringTypeFullName = declaringType?.FullName?.ToUpperInvariant() ?? String.Empty;
        if (upperDeclaringTypeFullName.Contains("PUT"))
        {
            return "PUT";
        }
        if (upperDeclaringTypeFullName.Contains("PATCH"))
        {
            return "PATCH";
        }
        if (upperDeclaringTypeFullName.Contains("GET"))
        {
            return "GET";
        }

        return "UNKNOWN";
    }

    public static void LogOperationRequested<T, TResult>(ILogger<T> logger, Func<CancellationToken, Task<TResult>> action, string key)
    {
        int actionHash = action.GetHashCode();
        _executionStopwatches[actionHash] = Stopwatch.StartNew();

        logger.LogInformation(
            "Request #{}: {Method} key {Key} requested",
            actionHash,
            GetActionMethod(action.Method.DeclaringType),
            key);
    }

    public static void LogOperationStarting<T, TResult>(ILogger<T> logger, Func<CancellationToken, Task<TResult>> action, string key)
    {
        int actionHash = action.GetHashCode();
        _executionStopwatches[actionHash].Stop();

        logger.LogInformation(
            "Request #{}: {Method} key {Key} started {} ms after request",
            actionHash,
            GetActionMethod(action.Method.DeclaringType),
            key,
            _executionStopwatches[actionHash].ElapsedMilliseconds);

        _executionStopwatches[actionHash].Restart();
    }

    public static void LogOperationCompleted<T, TResult>(ILogger<T> logger, Func<CancellationToken, Task<TResult>> action, string key)
    {
        int actionHash = action.GetHashCode();

        _executionStopwatches.Remove(actionHash, out Stopwatch? stopwatch);
        stopwatch?.Stop();

        logger.LogInformation(
            "Request #{}: {Method} key {Key} completed in {} ms after start",
            actionHash,
            GetActionMethod(action.Method.DeclaringType),
            key,
            stopwatch!.ElapsedMilliseconds);
        
    }
}
