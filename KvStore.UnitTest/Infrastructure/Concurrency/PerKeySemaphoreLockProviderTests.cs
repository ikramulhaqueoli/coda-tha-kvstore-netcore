using KvStore.Infrastructure.Concurrency;

namespace KvStore.UnitTest.Infrastructure.Concurrency;

public sealed class PerKeySemaphoreLockProviderTests
{
    [Fact]
    public async Task ExecuteWithLockAsync_SerializesWorkPerKey()
    {
        using var provider = new PerKeySemaphoreLockProvider();
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
}

