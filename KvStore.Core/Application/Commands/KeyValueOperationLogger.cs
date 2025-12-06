using Microsoft.Extensions.Logging;

namespace KvStore.Core.Application.Commands;

public static class KeyValueOperationLogger
{
    public static void LogOperationRequested<T>(ILogger<T> logger, string actionName, string key)
    {
        logger.LogInformation(
            "Action {ActionName} on key {Key} requested at {RequestedAt}",
            actionName,
            key,
            DateTimeOffset.UtcNow);
    }

    public static void LogOperationStarting<T>(ILogger<T> logger, string actionName, string key)
    {
        logger.LogInformation(
            "Action {ActionName} on key {Key} starting at {ExecutionStart}",
            actionName,
            key,
            DateTimeOffset.UtcNow);
    }

    public static void LogOperationCompleted<T>(ILogger<T> logger, string actionName, string key)
    {
        logger.LogInformation(
            "Action {ActionName} on key {Key} completed at {CompletedAt}",
            actionName,
            key,
            DateTimeOffset.UtcNow);
    }
}
