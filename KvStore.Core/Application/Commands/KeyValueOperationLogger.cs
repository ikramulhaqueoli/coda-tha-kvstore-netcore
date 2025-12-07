using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KvStore.Core.Application.Commands;

public static class KeyValueOperationLogger
{
    private static long GetMillisecondTimestamp()
    {
        var timestamp = Stopwatch.GetTimestamp();
        var milliseconds = (timestamp * 1_000L) / Stopwatch.Frequency;
        return milliseconds;
    }

    private static string GetActionName(string methodName)
    {
        var upperMethodName = methodName.ToUpperInvariant();
        if (upperMethodName.Contains("PUT"))
        {
            return "PUT";
        }
        if (upperMethodName.Contains("PATCH"))
        {
            return "PATCH";
        }
        if (upperMethodName.Contains("GET"))
        {
            return "GET";
        }

        return methodName;
    }

    public static void LogOperationRequested<T>(ILogger<T> logger, string methodName, string key)
    {
        logger.LogInformation(
            "Action {ActionName} on key {Key} requested at {RequestedAt}",
            GetActionName(methodName),
            key,
            GetMillisecondTimestamp());
    }

    public static void LogOperationStarting<T>(ILogger<T> logger, string methodName, string key)
    {
        logger.LogInformation(
            "Action {ActionName} on key {Key} starting at {ExecutionStart}",
            GetActionName(methodName),
            key,
            GetMillisecondTimestamp());
    }

    public static void LogOperationCompleted<T>(ILogger<T> logger, string methodName, string key)
    {
        logger.LogInformation(
            "Action {ActionName} on key {Key} completed at {CompletedAt}",
            GetActionName(methodName),
            key,
            GetMillisecondTimestamp());
    }
}
