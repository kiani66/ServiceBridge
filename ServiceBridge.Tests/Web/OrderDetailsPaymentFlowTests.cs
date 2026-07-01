using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Web.Pages.Orders;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class OrderDetailsPaymentFlowTests
{
    [Fact]
    public async Task OnPostInitiatePaymentAsync_WhenMockSucceeds_RedirectsToPaymentSuccess()
    {
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var requestedEndpoints = new List<string>();
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedEndpoints.Add(request.RequestUri!.PathAndQuery);

            if (request.RequestUri!.PathAndQuery == "/api/payments/initiate")
            {
                return Task.FromResult(JsonResponse(
                    HttpStatusCode.Created,
                    ServiceResult<PaymentInitiatedResponse>.Created(
                        new PaymentInitiatedResponse { PaymentId = paymentId },
                        "Payment initiated successfully.")));
            }

            if (request.RequestUri!.PathAndQuery == "/api/payments/mock")
            {
                return Task.FromResult(JsonResponse(
                    HttpStatusCode.OK,
                    ServiceResult<MockPaymentResultResponse>.Ok(
                        new MockPaymentResultResponse
                        {
                            PaymentId = paymentId,
                            OrderId = orderId,
                            Status = PaymentStatus.Completed,
                            GatewayReference = "mock-success"
                        },
                        "Payment confirmed successfully.")));
            }

            throw new InvalidOperationException(
                $"Unexpected endpoint {request.RequestUri!.PathAndQuery}");
        });
        var model = CreateModel(handler);

        var result = await model.OnPostInitiatePaymentAsync(orderId);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(
            $"/ui/payments/success/{orderId}?paymentId={paymentId}",
            redirect.Url);
        Assert.Equal(
            ["/api/payments/initiate", "/api/payments/mock"],
            requestedEndpoints);
    }

    [Fact]
    public async Task OnPostInitiatePaymentAsync_WhenMockFails_ShowsMockError()
    {
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var requestedEndpoints = new List<string>();
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedEndpoints.Add(request.RequestUri!.PathAndQuery);

            if (request.RequestUri!.PathAndQuery == "/api/payments/initiate")
            {
                return Task.FromResult(JsonResponse(
                    HttpStatusCode.OK,
                    ServiceResult<PaymentInitiatedResponse>.Ok(
                        new PaymentInitiatedResponse { PaymentId = paymentId },
                        "Payment initiated successfully.")));
            }

            if (request.RequestUri!.PathAndQuery == "/api/payments/mock")
            {
                return Task.FromResult(JsonResponse(
                    HttpStatusCode.Conflict,
                    ServiceResult<MockPaymentResultResponse>.Fail(
                        "The payment state changed. Refresh and try again.",
                        AppConstants.ErrorCodes.Conflict,
                        409)));
            }

            if (request.RequestUri!.PathAndQuery == $"/api/orders/my/{orderId}")
            {
                return Task.FromResult(JsonResponse(
                    HttpStatusCode.OK,
                    ServiceResult<UserOrderDetailDto>.Ok(new UserOrderDetailDto
                    {
                        OrderId = orderId,
                        OrderNumber = "SB-100",
                        ServiceName = "Test service",
                        Status = nameof(OrderStatus.WaitingForPayment)
                    })));
            }

            throw new InvalidOperationException(
                $"Unexpected endpoint {request.RequestUri!.PathAndQuery}");
        });
        var model = CreateModel(handler);

        var result = await model.OnPostInitiatePaymentAsync(orderId);

        Assert.IsType<PageResult>(result);
        Assert.Equal(
            "The payment state changed. Refresh and try again.",
            model.ErrorMessage);
        Assert.Equal(
            [
                "/api/payments/initiate",
                "/api/payments/mock",
                $"/api/orders/my/{orderId}"
            ],
            requestedEndpoints);
    }

    private static DetailsModel CreateModel(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(new StubAuthenticationService());
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, AppConstants.Roles.User)
            ], "Test"))
        };

        return new DetailsModel(new StubHttpClientFactory(handler))
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new StubTempDataProvider())
        };
    }

    private static HttpResponseMessage JsonResponse<T>(
        HttpStatusCode status,
        T value)
    {
        return new HttpResponseMessage(status) { Content = JsonContent.Create(value) };
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler) { BaseAddress = new Uri("https://localhost/") };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responseFactory(request);
    }

    private sealed class StubTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }

    private sealed class StubAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(
            HttpContext context,
            string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task ForbidAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignOutAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) => Task.CompletedTask;
    }
}
