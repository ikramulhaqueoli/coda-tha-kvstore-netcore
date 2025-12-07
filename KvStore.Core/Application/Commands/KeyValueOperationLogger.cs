using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace KvStore.Core.Application.Commands;

public static class KeyValueOperationLogger
{
    private static long GetTimestampMs()
    {
        var timestamp = Stopwatch.GetTimestamp();
        var milliseconds = (timestamp * 1_000L) / Stopwatch.Frequency;
        return milliseconds;
    }

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
        logger.LogInformation(
            "Request #{}: {Method} key {Key} requested at Timestamp {RequestedAt}",
            action.GetHashCode(),
            GetActionMethod(action.Method.DeclaringType),
            key,
            GetTimestampMs());
    }

    public static void LogOperationStarting<T, TResult>(ILogger<T> logger, Func<CancellationToken, Task<TResult>> action, string key)
    {
        logger.LogInformation(
            "Request #{}: {Method} key {Key} started at Timestamp {ExecutionStart}",
            action.GetHashCode(),
            GetActionMethod(action.Method.DeclaringType),
            key,
            GetTimestampMs());
    }

    public static void LogOperationCompleted<T, TResult>(ILogger<T> logger, Func<CancellationToken, Task<TResult>> action, string key)
    {
        logger.LogInformation(
            "Request #{}: {Method} key {Key} completed at Timestamp {CompletedAt}",
            action.GetHashCode(),
            GetActionMethod(action.Method.DeclaringType),
            key,
            GetTimestampMs());
    }
}
