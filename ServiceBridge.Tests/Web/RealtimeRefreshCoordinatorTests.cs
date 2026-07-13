using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class RealtimeRefreshCoordinatorTests
{
    [Fact]
    public async Task RunAsync_CoalescesOverlappingRequestsWithoutOverlap()
    {
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal();
        var runs = 0;
        var concurrent = 0;
        var maximumConcurrent = 0;
        using var coordinator = new RealtimeRefreshCoordinator(async () =>
        {
            var run = Interlocked.Increment(ref runs);
            var active = Interlocked.Increment(ref concurrent);
            maximumConcurrent = Math.Max(maximumConcurrent, active);
            try
            {
                if (run == 1)
                {
                    firstStarted.TrySetResult();
                    await releaseFirst.Task;
                }
            }
            finally
            {
                Interlocked.Decrement(ref concurrent);
            }
        });

        var first = coordinator.RunAsync();
        await firstStarted.Task;
        var second = coordinator.RunAsync();
        var third = coordinator.RunAsync();
        releaseFirst.TrySetResult();

        await Task.WhenAll(first, second, third);

        Assert.Equal(2, runs);
        Assert.Equal(1, maximumConcurrent);
    }

    [Fact]
    public async Task DisposeDuringActiveLoad_DiscardsPendingAndPreventsStateOrRenderApplication()
    {
        var started = NewSignal();
        var release = NewSignal();
        var runs = 0;
        var appliedState = 0;
        var renderDispatches = 0;
        RealtimeRefreshCoordinator? coordinator = null;
        coordinator = new RealtimeRefreshCoordinator(async () =>
        {
            Interlocked.Increment(ref runs);
            started.TrySetResult();
            await release.Task;
            if (coordinator!.TryApply(() => appliedState = 1))
            {
                renderDispatches++;
            }
        });

        var active = coordinator.RunAsync();
        await started.Task;
        var pending = coordinator.RunAsync();
        coordinator.Dispose();
        coordinator.Dispose();
        release.TrySetResult();

        await Task.WhenAll(active, pending);
        await coordinator.RunAsync();

        Assert.True(coordinator.IsDisposed);
        Assert.Equal(1, runs);
        Assert.Equal(0, appliedState);
        Assert.Equal(0, renderDispatches);
    }

    [Fact]
    public async Task FailedActiveRefresh_RunsPendingAndDoesNotBlockLaterRefresh()
    {
        var started = NewSignal();
        var release = NewSignal();
        var runs = 0;
        var appliedState = 0;
        RealtimeRefreshCoordinator? coordinator = null;
        coordinator = new RealtimeRefreshCoordinator(async () =>
        {
            var run = Interlocked.Increment(ref runs);
            if (run == 1)
            {
                started.TrySetResult();
                await release.Task;
                throw new InvalidOperationException("simulated reload failure");
            }

            coordinator!.TryApply(() => appliedState = run);
        });

        var active = coordinator.RunAsync();
        await started.Task;
        var pending = coordinator.RunAsync();
        release.TrySetResult();

        await Assert.ThrowsAsync<InvalidOperationException>(() => active);
        await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
        Assert.Equal(2, runs);
        Assert.Equal(2, appliedState);

        await coordinator.RunAsync();

        Assert.Equal(3, runs);
        Assert.Equal(3, appliedState);
        coordinator.Dispose();
    }

    [Fact]
    public void RoleOrderDetails_UseSharedDisposableApplicationGate()
    {
        foreach (var page in new[]
                 {
                     "OrderDetails.razor",
                     "AgentOrderWorkspace.razor",
                     "AdminOrderManagement.razor"
                 })
        {
            var source = File.ReadAllText(PagePath(page));
            Assert.Contains(
                "new RealtimeRefreshCoordinator(RefreshFromRealtimeCoreAsync)",
                source);
            Assert.Contains("private bool _disposed;", source);
            Assert.Contains("_realtimeRefresh?.RunAsync()", source);
            Assert.Contains("_realtimeRefresh?.TryApply(apply)", source);
            Assert.Contains("_realtimeRefresh?.Dispose();", source);
            Assert.Equal(
                1,
                source.Split("SubscribeToOrderUpdates", StringSplitOptions.None).Length - 1);
        }
    }

    private static TaskCompletionSource NewSignal()
    {
        return new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static string PagePath(string page)
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            ".."));
        return Path.Combine(
            root,
            "ServiceBridge.Web",
            "Components",
            "Pages",
            page);
    }
}
