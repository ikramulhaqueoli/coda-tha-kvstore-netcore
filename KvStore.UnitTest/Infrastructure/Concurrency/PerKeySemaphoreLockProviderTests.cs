using KvStore.Infrastructure.Concurrency;
using KvStore.UnitTest.TestHelpers;

namespace KvStore.UnitTest.Infrastructure.Concurrency;

public sealed class PerKeySemaphoreLockProviderTests
{
    [Fact]
    public async Task ExecuteWithLockAsync_SerializesWorkPerKey()
    {
        using var provider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var firstEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowFirstToFinish = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstTask = provider.ExecuteWithLockAsync("alpha", async _ =>
        {
            firstEntered.TrySetResult(true);
            await allowFirstToFinish.Task;
            return 1;
        }, CancellationToken.None);

        var secondTask = provider.ExecuteWithLockAsync("alpha", _ =>
        {
            secondStarted.TrySetResult(true);
            return Task.FromResult(2);
        }, CancellationToken.None);

        await firstEntered.Task;
        Assert.False(secondStarted.Task.IsCompleted);

        allowFirstToFinish.TrySetResult(true);
        await Task.WhenAll(firstTask, secondTask);

        await secondStarted.Task;
    }

    [Fact]
    public async Task ExecuteWithLockAsync_AllowsParallelExecution_ForDifferentKeys()
    {
        using var provider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var key1Entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var key2Entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowBothToFinish = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var task1 = provider.ExecuteWithLockAsync("key1", async _ =>
        {
            key1Entered.TrySetResult(true);
            await allowBothToFinish.Task;
            return 1;
        }, CancellationToken.None);

        var task2 = provider.ExecuteWithLockAsync("key2", async _ =>
        {
            key2Entered.TrySetResult(true);
            await allowBothToFinish.Task;
            return 2;
        }, CancellationToken.None);

        await Task.WhenAll(key1Entered.Task, key2Entered.Task);

        allowBothToFinish.TrySetResult(true);
        var results = await Task.WhenAll(task1, task2);

        Assert.Equal(1, results[0]);
        Assert.Equal(2, results[1]);
    }

    [Fact]
    public async Task ExecuteWithLockAsync_PropagatesExceptions()
    {
        using var provider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provider.ExecuteWithLockAsync("key", _ =>
            {
                throw new InvalidOperationException("Test exception");
            }, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteWithLockAsync_RespectsCancellationToken()
    {
        using var provider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());
        var cts = new CancellationTokenSource();

        cts.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await provider.ExecuteWithLockAsync<int>("key", async _ =>
            {
                await Task.Delay(100, cts.Token);
                return 1;
            }, cts.Token));
        
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ExecuteWithLockAsync_ReturnsResult()
    {
        using var provider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());

        var result = await provider.ExecuteWithLockAsync("key", _ =>
            Task.FromResult(42), CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteWithLockAsync_HandlesMultipleSequentialCalls()
    {
        using var provider = new PerKeySemaphoreLockProvider(new TestLogger<PerKeySemaphoreLockProvider>());

        var result1 = await provider.ExecuteWithLockAsync("key", _ =>
            Task.FromResult(1), CancellationToken.None);

        var result2 = await provider.ExecuteWithLockAsync("key", _ =>
            Task.FromResult(2), CancellationToken.None);

        Assert.Equal(1, result1);
        Assert.Equal(2, result2);
    }
}

