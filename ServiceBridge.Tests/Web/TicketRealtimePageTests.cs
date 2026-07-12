using Xunit;

namespace ServiceBridge.Tests.Web;

public class TicketRealtimePageTests
{
    [Theory]
    [InlineData("Tickets.razor")]
    [InlineData("TicketDetails.razor")]
    [InlineData("AdminTickets.razor")]
    [InlineData("AdminTicketDetails.razor")]
    public void TicketPages_RefreshThroughSharedRealtimeClient(string pageName)
    {
        var source = File.ReadAllText(WebPagePath(pageName));

        Assert.Contains("@inject RealtimeClient RealtimeClient", source);
        Assert.Contains("RealtimeClient.SubscribeToTicketUpdates(HandleTicketUpdatedAsync)", source);
        Assert.Contains("_realtimeSubscriptions.Dispose();", source);
        Assert.DoesNotContain("RealtimeClient.StartAsync", source);
        Assert.DoesNotContain("new RealtimeClient", source);
    }

    private static string WebPagePath(string pageName)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "ServiceBridge.Web",
            "Components",
            "Pages",
            pageName));
    }
}
