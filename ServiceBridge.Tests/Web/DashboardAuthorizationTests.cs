using Microsoft.AspNetCore.Authorization;
using ServiceBridge.Shared.Constants;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class DashboardAuthorizationTests
{
    [Theory]
    [InlineData(
        typeof(ServiceBridge.Web.Pages.Admin.Orders.IndexModel),
        AppConstants.Roles.Admin)]
    [InlineData(
        typeof(ServiceBridge.Web.Pages.Agent.Orders.IndexModel),
        AppConstants.Roles.Agent)]
    [InlineData(
        typeof(ServiceBridge.Web.Pages.Agent.Orders.DetailsModel),
        AppConstants.Roles.Agent)]
    [InlineData(
        typeof(ServiceBridge.Web.Pages.Admin.Users.IndexModel),
        AppConstants.Roles.Admin)]
    [InlineData(
        typeof(ServiceBridge.Web.Pages.Admin.Catalog.IndexModel),
        AppConstants.Roles.Admin)]
    [InlineData(
        typeof(ServiceBridge.Web.Pages.Admin.Catalog.ServiceModel),
        AppConstants.Roles.Admin)]
    [InlineData(
        typeof(ServiceBridge.Web.Pages.Admin.Pricing.IndexModel),
        AppConstants.Roles.Admin)]
    [InlineData(
        typeof(ServiceBridge.Web.Pages.Orders.IndexModel),
        AppConstants.Roles.User)]
    [InlineData(
        typeof(ServiceBridge.Web.Pages.Orders.DetailsModel),
        AppConstants.Roles.User)]
    public void OperationsPages_RequireExpectedRole(
        Type pageModelType,
        string expectedRole)
    {
        var attribute = Assert.Single(
            pageModelType
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(expectedRole, attribute.Roles);
    }

    [Fact]
    public void Dashboard_RequiresAdminRole()
    {
        var attribute = Assert.Single(
            typeof(ServiceBridge.Web.Pages.DashboardModel)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(AppConstants.Roles.Admin, attribute.Roles);
    }
}
