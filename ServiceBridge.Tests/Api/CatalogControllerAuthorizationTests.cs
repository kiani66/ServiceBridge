using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using ServiceBridge.Api.Controllers;
using ServiceBridge.Shared.Constants;
using Xunit;

namespace ServiceBridge.Tests.Api;

public class CatalogControllerAuthorizationTests
{
    [Fact]
    public void AdminCatalogController_RequiresAdminRole()
    {
        var attribute = Assert.Single(
            typeof(AdminCatalogController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(AppConstants.Roles.Admin, attribute.Roles);
    }

    [Fact]
    public void ServicesController_AllowsAnonymousAccess()
    {
        Assert.Single(
            typeof(ServicesController)
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }

    [Theory]
    [InlineData(typeof(ServicesController), "api/services")]
    [InlineData(typeof(AdminCatalogController), "api/admin/catalog")]
    [InlineData(typeof(PricingController), "api/services/{serviceId:guid}/quote")]
    [InlineData(typeof(AdminPricingController), "api/admin/pricing")]
    [InlineData(typeof(OrdersController), "api/orders")]
    public void CatalogControllers_UseExpectedRoutes(Type controllerType, string expectedRoute)
    {
        var route = Assert.Single(
            controllerType
                .GetCustomAttributes(typeof(RouteAttribute), false)
                .Cast<RouteAttribute>());

        Assert.Equal(expectedRoute, route.Template);
    }

    [Fact]
    public void AdminPricingController_RequiresAdminRole()
    {
        var attribute = Assert.Single(
            typeof(AdminPricingController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(AppConstants.Roles.Admin, attribute.Roles);
    }

    [Fact]
    public void PricingController_AllowsAnonymousAccess()
    {
        Assert.Single(
            typeof(PricingController)
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }

    [Fact]
    public void OrdersController_RequiresUserRole()
    {
        var attribute = Assert.Single(
            typeof(OrdersController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(AppConstants.Roles.User, attribute.Roles);
    }

    [Theory]
    [InlineData(nameof(OrdersController.GetMyOrders), "my")]
    [InlineData(nameof(OrdersController.GetMyOrder), "my/{orderId:guid}")]
    [InlineData(nameof(OrdersController.CancelMyOrder), "my/{orderId:guid}/cancel")]
    public void UserOrderEndpoints_UseExpectedRoutes(
        string methodName,
        string expectedTemplate)
    {
        var method = typeof(OrdersController).GetMethod(methodName)!;
        var route = Assert.Single(
            method.GetCustomAttributes(typeof(HttpMethodAttribute), false)
                .Cast<HttpMethodAttribute>());

        Assert.Equal(expectedTemplate, route.Template);
    }

    [Fact]
    public void PaymentInitiation_RequiresUserRole()
    {
        var method = typeof(PaymentsController)
            .GetMethod(nameof(PaymentsController.Initiate))!;
        var attribute = Assert.Single(
            method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(AppConstants.Roles.User, attribute.Roles);
    }

    [Fact]
    public void PaymentCallback_AllowsAnonymousAccess()
    {
        var method = typeof(PaymentsController)
            .GetMethod(nameof(PaymentsController.Callback))!;

        Assert.Single(
            method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }

    [Fact]
    public void AdminOrdersController_RequiresAdminRole()
    {
        var attribute = Assert.Single(
            typeof(AdminOrdersController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(AppConstants.Roles.Admin, attribute.Roles);
    }

    [Fact]
    public void AgentOrdersController_RequiresAgentRole()
    {
        var attribute = Assert.Single(
            typeof(AgentOrdersController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(AppConstants.Roles.Agent, attribute.Roles);
    }

    [Fact]
    public void AdminUsersController_RequiresAdminRole()
    {
        var attribute = Assert.Single(
            typeof(AdminUsersController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(AppConstants.Roles.Admin, attribute.Roles);
    }

    [Fact]
    public void AdminResetPassword_UsesExpectedRoute()
    {
        var method = typeof(AdminUsersController)
            .GetMethod(nameof(AdminUsersController.ResetPassword))!;
        var route = Assert.Single(
            method.GetCustomAttributes(typeof(HttpMethodAttribute), false)
                .Cast<HttpMethodAttribute>());

        Assert.Equal("{userId:guid}/reset-password", route.Template);
    }

    [Fact]
    public void AdminGetUsers_UsesControllerRootRoute()
    {
        var method = typeof(AdminUsersController)
            .GetMethod(nameof(AdminUsersController.GetUsers))!;
        var route = Assert.Single(
            method.GetCustomAttributes(typeof(HttpMethodAttribute), false)
                .Cast<HttpMethodAttribute>());

        Assert.Null(route.Template);
    }

    [Theory]
    [InlineData(nameof(AdminUsersController.CreateAgent), "agents")]
    [InlineData(nameof(AdminUsersController.SetStatus), "{userId:guid}/status")]
    public void AdminUserManagementEndpoints_UseExpectedRoutes(
        string methodName,
        string expectedTemplate)
    {
        var method = typeof(AdminUsersController).GetMethod(methodName)!;
        var route = Assert.Single(
            method.GetCustomAttributes(typeof(HttpMethodAttribute), false)
                .Cast<HttpMethodAttribute>());

        Assert.Equal(expectedTemplate, route.Template);
    }

    [Fact]
    public void AuthChangePassword_RequiresAuthentication()
    {
        var method = typeof(AuthController)
            .GetMethod(nameof(AuthController.ChangePassword))!;

        Assert.Single(
            method.GetCustomAttributes(typeof(AuthorizeAttribute), true));
    }

    [Theory]
    [InlineData(nameof(AgentOrdersController.GetAssignedOrders), null)]
    [InlineData(nameof(AgentOrdersController.GetAssignedOrder), "{orderId:guid}")]
    [InlineData(nameof(AgentOrdersController.StartProcessing), "{orderId:guid}/start")]
    [InlineData(
        nameof(AgentOrdersController.RequestCustomerAction),
        "{orderId:guid}/request-customer-action")]
    [InlineData(nameof(AgentOrdersController.ResumeProcessing), "{orderId:guid}/resume")]
    [InlineData(nameof(AgentOrdersController.Complete), "{orderId:guid}/complete")]
    [InlineData(nameof(AgentOrdersController.Fail), "{orderId:guid}/fail")]
    [InlineData(nameof(AgentOrdersController.AddNote), "{orderId:guid}/notes")]
    public void AgentOrderEndpoints_UseExpectedRoutes(
        string methodName,
        string? expectedTemplate)
    {
        var method = typeof(AgentOrdersController).GetMethod(methodName)!;
        var route = Assert.Single(
            method.GetCustomAttributes(typeof(HttpMethodAttribute), false)
                .Cast<HttpMethodAttribute>());

        Assert.Equal(expectedTemplate, route.Template);
    }
}
