namespace KvStore.Core.Application.Abstractions;

public interface IKeyLockProvider
{
    Task<TResult> ExecuteWithLockAsync<TResult>(string key, Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken);
}

