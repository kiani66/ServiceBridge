using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class CoalescingAsyncActionTests
{
    [Fact]
    public async Task RequestsDuringRefresh_CollapseIntoOnePendingRefresh()
    {
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal();
        var secondStarted = NewSignal();
        var releaseSecond = NewSignal();
        var calls = 0;

        using var action = new CoalescingAsyncAction(async () =>
        {
            var call = Interlocked.Increment(ref calls);
            if (call == 1)
            {
                firstStarted.TrySetResult();
                await releaseFirst.Task;
            }
            else if (call == 2)
            {
                secondStarted.TrySetResult();
                await releaseSecond.Task;
            }
        });

        var initialRequest = action.RequestAsync();
        await firstStarted.Task;

        var pendingRequest1 = action.RequestAsync();
        var pendingRequest2 = action.RequestAsync();
        releaseFirst.TrySetResult();

        await secondStarted.Task;
        Assert.Equal(2, Volatile.Read(ref calls));

        releaseSecond.TrySetResult();
        await Task.WhenAll(initialRequest, pendingRequest1, pendingRequest2);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Dispose_IgnoresNewAndPendingRefreshRequests()
    {
        var started = NewSignal();
        var release = NewSignal();
        var calls = 0;
        var action = new CoalescingAsyncAction(async () =>
        {
            Interlocked.Increment(ref calls);
            started.TrySetResult();
            await release.Task;
        });

        var runningRequest = action.RequestAsync();
        await started.Task;
        var pendingRequest = action.RequestAsync();

        action.Dispose();
        release.TrySetResult();
        await Task.WhenAll(runningRequest, pendingRequest);
        await action.RequestAsync();

        Assert.Equal(1, calls);
    }

    private static TaskCompletionSource NewSignal()
    {
        return new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
