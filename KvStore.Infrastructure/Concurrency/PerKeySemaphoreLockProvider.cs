using System.Collections.Concurrent;
using KvStore.Core.Application.Abstractions;

namespace KvStore.Infrastructure.Concurrency;

public sealed class PerKeySemaphoreLockProvider : IKeyLockProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);
    private bool _disposed;

    public async Task<TResult> ExecuteWithLockAsync<TResult>(
        string key,
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(PerKeySemaphoreLockProvider));
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(action);

        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(initialCount: 1, maxCount: 1));
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action(cancellationToken).ConfigureAwait(false);
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

