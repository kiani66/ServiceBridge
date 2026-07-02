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
        "/ui/admin|/Admin/Catalog|/Admin/Pricing|/Admin/Users|/Admin/Orders|/ui/admin/tickets|/ui/profile")]
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
