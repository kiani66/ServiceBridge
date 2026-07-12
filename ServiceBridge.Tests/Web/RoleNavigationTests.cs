using System.Security.Claims;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Web.Navigation;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class RoleNavigationTests
{
    [Theory]
    [InlineData(
        AppConstants.Roles.User,
        "Services|Orders|Support|Profile",
        "/ui/catalog|/ui/orders|/ui/tickets|/ui/profile")]
    [InlineData(
        AppConstants.Roles.Agent,
        "Assigned Orders|Processing|Completed|Profile",
        "/Agent/Orders?status=Assigned|/Agent/Orders?status=Processing|/Agent/Orders?status=Completed|/ui/profile")]
    [InlineData(
        AppConstants.Roles.Admin,
        "Dashboard|Catalog|Pricing|Users|Orders|Support|Profile",
        "/ui/admin|/ui/admin/catalog|/ui/admin/pricing|/ui/admin/users|/ui/admin/orders|/ui/admin/tickets|/ui/profile")]
    public void GetItems_ReturnsOnlyItemsForRole(
        string role,
        string expectedText,
        string expectedPages)
    {
        var items = RoleNavigation.GetItems(CreateUser(role));

        Assert.Equal(expectedText.Split('|'), items.Select(x => x.Text));
        Assert.Equal(expectedPages.Split('|'), items.Select(x => x.Page));
    }

    [Theory]
    [InlineData(AppConstants.Roles.User, "/ui/orders")]
    [InlineData(AppConstants.Roles.Agent, "/Agent/Orders")]
    [InlineData(AppConstants.Roles.Admin, "/ui/admin")]
    public void GetLandingPage_ReturnsTaskWorkspace(
        string role,
        string expectedPage)
    {
        Assert.Equal(expectedPage, RoleNavigation.GetLandingPage(role));
        Assert.Equal(expectedPage, RoleNavigation.GetLandingPage(CreateUser(role)));
    }

    [Fact]
    public void GetItems_UnauthenticatedUser_HasNoRoleNavigation()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Empty(RoleNavigation.GetItems(user));
    }

    [Fact]
    public void GetConsoleItems_AdminSeesAuditLogsLink()
    {
        var items = RoleNavigation.GetConsoleItems(CreateUser(AppConstants.Roles.Admin));

        Assert.Contains(items, item =>
            item.Text == "Audit Logs" &&
            item.Page == "/ui/admin/audit-logs");
    }

    [Theory]
    [InlineData(AppConstants.Roles.User)]
    [InlineData(AppConstants.Roles.Agent)]
    public void GetConsoleItems_NonAdminsDoNotSeeAuditLogsLink(string role)
    {
        var items = RoleNavigation.GetConsoleItems(CreateUser(role));

        Assert.DoesNotContain(items, item => item.Page == "/ui/admin/audit-logs");
        Assert.DoesNotContain(items, item => item.Page == "/ui/admin/refunds");
    }

    [Fact]
    public void GetConsoleItems_UserStartsWithDashboardAndUsesSharedWorkspaceGroups()
    {
        var items = RoleNavigation.GetConsoleItems(CreateUser(AppConstants.Roles.User));

        Assert.Equal("Dashboard", items[0].Text);
        Assert.Equal("/ui/dashboard", items[0].Page);
        Assert.All(items.Take(4), item => Assert.Equal("Workspace", item.Group));
        Assert.Equal("/ui/dashboard", RoleNavigation.GetConsoleLandingPage(AppConstants.Roles.User));
    }

    [Fact]
    public void GetConsoleItems_AgentHasOneOrdersNavigationEntry()
    {
        var items = RoleNavigation.GetConsoleItems(CreateUser(AppConstants.Roles.Agent));

        var orders = items.Where(item => item.Page.StartsWith("/ui/agent/orders", StringComparison.Ordinal)).ToArray();
        Assert.Single(orders);
        Assert.Equal("Orders", orders[0].Text);
    }

    [Fact]
    public void UnknownRole_HasNoNavigationOrLandingPage()
    {
        Assert.Empty(RoleNavigation.GetItems(CreateUser("Unknown")));
        Assert.Null(RoleNavigation.GetLandingPage("Unknown"));
    }

    private static ClaimsPrincipal CreateUser(string role)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, role)
        ], "Test"));
    }
}
