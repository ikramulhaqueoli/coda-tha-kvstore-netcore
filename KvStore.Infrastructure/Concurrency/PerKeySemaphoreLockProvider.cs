using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Commands;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace KvStore.Infrastructure.Concurrency;

public sealed class PerKeySemaphoreLockProvider(ILogger<PerKeySemaphoreLockProvider> logger) : IKeyLockProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);
    private bool _disposed;

    public async Task<TResult> ExecuteWithLockAsync<TResult>(
        string key,
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken)
    {
        KeyValueOperationLogger.LogOperationRequested(logger, nameof(action), key);

        ObjectDisposedException.ThrowIf(_disposed, typeof(PerKeySemaphoreLockProvider));
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(action);

        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(initialCount: 1, maxCount: 1));
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            KeyValueOperationLogger.LogOperationStarting(logger, nameof(action), key);
            var result = await action(cancellationToken).ConfigureAwait(false);
            KeyValueOperationLogger.LogOperationCompleted(logger, nameof(action), key);
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var semaphore in _locks.Values)
        {
            semaphore.Dispose();
        }

        _locks.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

