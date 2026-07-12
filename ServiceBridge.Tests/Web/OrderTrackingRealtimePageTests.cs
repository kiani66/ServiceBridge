using Xunit;

namespace ServiceBridge.Tests.Web;

public class OrderTrackingRealtimePageTests
{
    [Theory]
    [InlineData("Orders.razor")]
    [InlineData("OrderDetails.razor")]
    [InlineData("PaymentSuccess.razor")]
    [InlineData("PaymentFailure.razor")]
    public void OrderTrackingPages_ReuseSharedRealtimeClientSubscriptions(string pageName)
    {
        var source = ReadPage(pageName);

        Assert.Contains("@inject RealtimeClient RealtimeClient", source);
        Assert.Contains("RealtimeClient.SubscribeToOrderUpdates(HandleOrderUpdatedAsync)", source);
        Assert.Contains(
            "RealtimeClient.SubscribeToCustomerActions(HandleCustomerActionChangedAsync)",
            source);
        Assert.Contains("_realtimeSubscriptions.Dispose();", source);
        Assert.DoesNotContain("RealtimeClient.StartAsync", source);
        Assert.DoesNotContain("new RealtimeClient", source);
    }

    [Theory]
    [InlineData("OrderDetails.razor")]
    [InlineData("PaymentSuccess.razor")]
    [InlineData("PaymentFailure.razor")]
    public void OrderDetailPages_FilterRealtimeEventsToCurrentOrder(string pageName)
    {
        var source = ReadPage(pageName);

        Assert.Contains("if (message.OrderId != OrderId)", source);
        Assert.Contains("return ReloadFromRealtimeAsync();", source);
    }

    private static string ReadPage(string pageName)
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(
            Path.Combine(root, "ServiceBridge.Web", "Components", "Pages", pageName));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ServiceBridge.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
