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
        var renderedState = 0;

        using var action = new CoalescingAsyncAction(async () =>
        {
            var call = Interlocked.Increment(ref calls);
            if (call == 1)
            {
                firstStarted.TrySetResult();
                await releaseFirst.Task;
                renderedState = 1;
            }
            else if (call == 2)
            {
                secondStarted.TrySetResult();
                await releaseSecond.Task;
                renderedState = 2;
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
        Assert.Equal(2, renderedState);
    }

    [Fact]
    public async Task FailedRefresh_RunsPendingRefreshAndAllowsLaterRequests()
    {
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal();
        var calls = 0;
        var latestState = 0;
        using var action = new CoalescingAsyncAction(async () =>
        {
            var call = Interlocked.Increment(ref calls);
            if (call == 1)
            {
                firstStarted.TrySetResult();
                await releaseFirst.Task;
                throw new InvalidOperationException("simulated refresh failure");
            }

            latestState = call;
        });

        var active = action.RequestAsync();
        await firstStarted.Task;
        var pending = action.RequestAsync();
        releaseFirst.TrySetResult();

        await Assert.ThrowsAsync<InvalidOperationException>(() => active);
        await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
        Assert.Equal(2, calls);
        Assert.Equal(2, latestState);

        await action.RequestAsync();

        Assert.Equal(3, calls);
        Assert.Equal(3, latestState);
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
