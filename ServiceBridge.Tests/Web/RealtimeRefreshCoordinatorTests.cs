using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class RealtimeRefreshCoordinatorTests
{
    [Fact]
    public async Task RunAsync_CoalescesRequestsWithoutOverlappingRefreshes()
    {
        var coordinator = new RealtimeRefreshCoordinator();
        var firstStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var runs = 0;
        var concurrent = 0;
        var maximumConcurrent = 0;

        async Task Refresh()
        {
            var run = Interlocked.Increment(ref runs);
            var active = Interlocked.Increment(ref concurrent);
            maximumConcurrent = Math.Max(maximumConcurrent, active);
            try
            {
                if (run == 1)
                {
                    firstStarted.SetResult();
                    await releaseFirst.Task;
                }
            }
            finally
            {
                Interlocked.Decrement(ref concurrent);
            }
        }

        var first = coordinator.RunAsync(Refresh);
        await firstStarted.Task;
        var second = coordinator.RunAsync(Refresh);
        var third = coordinator.RunAsync(Refresh);
        releaseFirst.SetResult();

        await Task.WhenAll(first, second, third);

        Assert.Equal(2, runs);
        Assert.Equal(1, maximumConcurrent);
    }

    [Fact]
    public void RoleOrderDetails_UseTheSharedRealtimeRefreshCoordinator()
    {
        foreach (var page in new[]
                 {
                     "OrderDetails.razor",
                     "AgentOrderWorkspace.razor",
                     "AdminOrderManagement.razor"
                 })
        {
            var source = File.ReadAllText(PagePath(page));
            Assert.Contains("RealtimeRefreshCoordinator _realtimeRefresh", source);
            Assert.Contains("_realtimeRefresh.RunAsync", source);
            Assert.Equal(
                1,
                source.Split("SubscribeToOrderUpdates", StringSplitOptions.None).Length - 1);
        }
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
