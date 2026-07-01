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
        "Services|Orders|Profile",
        "/Services/Index|/Orders/Index|/User/UserProfile")]
    [InlineData(
        AppConstants.Roles.Agent,
        "Dashboard|Profile",
        "/ui/agent|/User/UserProfile")]
    [InlineData(
        AppConstants.Roles.Admin,
        "Dashboard|Assignment Queue|Users|Catalog|Pricing|Profile",
        "/Dashboard|/Admin/Orders/Index|/Admin/Users/Index|/Admin/Catalog/Index|/Admin/Pricing/Index|/User/UserProfile")]
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
    [InlineData(AppConstants.Roles.User, "/Orders/Index")]
    [InlineData(AppConstants.Roles.Agent, "/ui/agent")]
    [InlineData(AppConstants.Roles.Admin, "/Dashboard")]
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
