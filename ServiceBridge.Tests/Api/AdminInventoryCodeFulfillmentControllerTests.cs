using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceBridge.Api.Controllers;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using Xunit;

namespace ServiceBridge.Tests.Api;

public sealed class AdminInventoryCodeFulfillmentControllerTests
{
    [Fact]
    public void RetryEndpoint_UsesExpectedAdminRouteAndAuthorization()
    {
        var authorization = Assert.Single(
            typeof(AdminOrdersController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());
        var method = typeof(AdminOrdersController).GetMethod(
            nameof(AdminOrdersController.RetryInventoryCodeFulfillment));
        var route = Assert.IsType<HttpPostAttribute>(
            method?.GetCustomAttributes(typeof(HttpPostAttribute), true).Single());

        Assert.Equal(AppConstants.Roles.Admin, authorization.Roles);
        Assert.Equal(
            "{orderId:guid}/inventory-fulfillment/retry",
            route.Template);
        Assert.Empty(
            method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }

    [Fact]
    public async Task AdminAuthorization_AllowsAuthenticatedAdmin()
    {
        var result = await EvaluateAdminPolicyAsync(AppConstants.Roles.Admin);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AdminAuthorization_ForbidsAuthenticatedNonAdmin()
    {
        var result = await EvaluateAdminPolicyAsync(AppConstants.Roles.User);

        Assert.True(result.Forbidden);
        Assert.False(result.Challenged);
    }

    [Fact]
    public async Task AdminAuthorization_ChallengesUnauthenticatedCaller()
    {
        var result = await EvaluateAdminPolicyAsync(null);

        Assert.True(result.Challenged);
        Assert.False(result.Forbidden);
    }

    [Fact]
    public async Task Retry_SuccessPassesIdentifiersAndReturnsExpectedDto()
    {
        var adminId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var dto = new RetryInventoryCodeFulfillmentResultDto
        {
            OrderId = orderId,
            OrderNumber = "ORD-RETRY-API",
            Outcome = InventoryCodeFulfillmentOutcome.Fulfilled,
            OrderStatus = "Completed"
        };
        var expected = ServiceResult<RetryInventoryCodeFulfillmentResultDto>.Ok(
            dto,
            "Inventory code fulfillment retry completed.");
        var service = new FakeAdminInventoryCodeFulfillmentService(expected);
        var controller = CreateController(service, adminId);

        var action = await controller.RetryInventoryCodeFulfillment(
            orderId,
            cancellationToken);

        var response = Assert.IsType<ObjectResult>(action);
        Assert.Equal(200, response.StatusCode);
        var payload = Assert.IsType<
            ServiceResult<RetryInventoryCodeFulfillmentResultDto>>(response.Value);
        Assert.Same(expected, payload);
        Assert.Same(dto, payload.Data);
        Assert.Equal(1, service.CallCount);
        Assert.Equal(adminId, service.AdminId);
        Assert.Equal(orderId, service.OrderId);
        Assert.Equal(cancellationToken, service.CancellationToken);
    }

    [Fact]
    public async Task Retry_MissingOrderReturnsExistingNotFoundResult()
    {
        var expected = ServiceResult<RetryInventoryCodeFulfillmentResultDto>.Fail(
            "Order not found.",
            AppConstants.ErrorCodes.NotFound,
            404);
        var service = new FakeAdminInventoryCodeFulfillmentService(expected);
        var controller = CreateController(service, Guid.NewGuid());

        var action = await controller.RetryInventoryCodeFulfillment(
            Guid.NewGuid(),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(action);
        Assert.Equal(404, response.StatusCode);
        Assert.Same(expected, response.Value);
        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public async Task Retry_RejectedRetryReturnsExistingConflictResult()
    {
        var expected = ServiceResult<RetryInventoryCodeFulfillmentResultDto>.Fail(
            "Inventory code fulfillment cannot be retried for this order.",
            AppConstants.ErrorCodes.Conflict,
            409);
        var service = new FakeAdminInventoryCodeFulfillmentService(expected);
        var controller = CreateController(service, Guid.NewGuid());

        var action = await controller.RetryInventoryCodeFulfillment(
            Guid.NewGuid(),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(action);
        Assert.Equal(409, response.StatusCode);
        Assert.Same(expected, response.Value);
        Assert.Equal(1, service.CallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task Retry_MissingOrInvalidAdminIdReturnsUnauthorizedWithoutServiceCall(
        string? nameIdentifier)
    {
        var service = new FakeAdminInventoryCodeFulfillmentService(
            ServiceResult<RetryInventoryCodeFulfillmentResultDto>.Ok(new()));
        var controller = CreateController(service, nameIdentifier);

        var action = await controller.RetryInventoryCodeFulfillment(
            Guid.NewGuid(),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(action);
        Assert.Equal(401, response.StatusCode);
        var payload = Assert.IsType<ServiceResult>(response.Value);
        Assert.False(payload.Success);
        Assert.Equal(AppConstants.ErrorCodes.Unauthorized, payload.ErrorCode);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public void RetryDto_ExposesOnlyApprovedOperationalFields()
    {
        var properties = typeof(RetryInventoryCodeFulfillmentResultDto)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(
            new[] { "OrderId", "OrderNumber", "OrderStatus", "Outcome" },
            properties);
    }

    private static AdminOrdersController CreateController(
        FakeAdminInventoryCodeFulfillmentService service,
        Guid adminId)
    {
        return CreateController(service, adminId.ToString());
    }

    private static AdminOrdersController CreateController(
        FakeAdminInventoryCodeFulfillmentService service,
        string? nameIdentifier)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, AppConstants.Roles.Admin)
        };
        if (nameIdentifier is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
        }

        var controller = new AdminOrdersController(null!, null!, service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(claims, "ControllerTest"))
                }
            }
        };
        return controller;
    }

    private static async Task<PolicyAuthorizationResult> EvaluateAdminPolicyAsync(
        string? role)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        await using var provider = services.BuildServiceProvider();
        var authorizationService =
            provider.GetRequiredService<IAuthorizationService>();
        var policyEvaluator = new PolicyEvaluator(authorizationService);
        var identity = role is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, role)],
                "AuthorizationTest");
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        var policy = new AuthorizationPolicyBuilder()
            .RequireRole(AppConstants.Roles.Admin)
            .Build();
        var authentication = identity.IsAuthenticated
            ? AuthenticateResult.Success(
                new AuthenticationTicket(principal, identity.AuthenticationType!))
            : AuthenticateResult.NoResult();

        return await policyEvaluator.AuthorizeAsync(
            policy,
            authentication,
            context,
            resource: null);
    }

    private sealed class FakeAdminInventoryCodeFulfillmentService :
        IAdminInventoryCodeFulfillmentService
    {
        private readonly ServiceResult<RetryInventoryCodeFulfillmentResultDto>
            _result;

        public FakeAdminInventoryCodeFulfillmentService(
            ServiceResult<RetryInventoryCodeFulfillmentResultDto> result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }
        public Guid AdminId { get; private set; }
        public Guid OrderId { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<ServiceResult<RetryInventoryCodeFulfillmentResultDto>>
            RetryAsync(
                Guid adminId,
                Guid orderId,
                CancellationToken cancellationToken = default)
        {
            CallCount++;
            AdminId = adminId;
            OrderId = orderId;
            CancellationToken = cancellationToken;
            return Task.FromResult(_result);
        }
    }
}
