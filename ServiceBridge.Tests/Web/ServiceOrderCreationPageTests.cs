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
using ServiceBridge.Shared.Constants;
using ServiceBridge.Web.Pages.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class ServiceOrderCreationPageTests
{
    [Fact]
    public async Task OnPost_MapsFieldsAndRedirectsToCreatedOrder()
    {
        var serviceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        CreateOrderRequest? capturedRequest = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedRequest = await request.Content!.ReadFromJsonAsync<CreateOrderRequest>();
            return JsonResponse(HttpStatusCode.Created, ServiceResult<OrderCreatedResponse>.Created(
                new OrderCreatedResponse { OrderId = orderId, OrderNumber = "PB-100" }));
        });
        var model = CreateModel(handler);
        model.OrderFields =
        [
            new DetailsModel.OrderFieldInput { FieldKey = "email", Value = " user@example.com " }
        ];

        var result = await model.OnPostAsync(serviceId);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Orders/Details", redirect.PageName);
        Assert.Equal(orderId, redirect.RouteValues!["id"]);
        Assert.NotNull(capturedRequest);
        Assert.Equal(serviceId, capturedRequest!.ServiceId);
        Assert.Equal("Purchase", capturedRequest.OrderType.ToString());
        var field = Assert.Single(capturedRequest.Credentials);
        Assert.Equal("email", field.FieldKey);
        Assert.Equal("user@example.com", field.Value);
    }

    [Fact]
    public async Task OnPost_AddsApiFieldValidationToModelState()
    {
        var serviceId = Guid.NewGuid();
        var calls = 0;
        var handler = new StubHttpMessageHandler(request =>
        {
            calls++;
            if (calls == 1)
            {
                return Task.FromResult(JsonResponse(
                    HttpStatusCode.BadRequest,
                    new ServiceResult<OrderCreatedResponse>
                    {
                        Success = false,
                        Message = "Validation failed.",
                        StatusCode = 400,
                        ValidationErrors = new Dictionary<string, string[]>
                        {
                            ["accountEmail"] = ["Account email is required."]
                        }
                    }));
            }

            return Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                ServiceResult<ServiceDetailResponse>.Ok(new ServiceDetailResponse
                {
                    Id = serviceId,
                    Name = "Test service",
                    RequiredFields =
                    [
                        new ServiceRequiredFieldResponse
                        {
                            FieldKey = "accountEmail",
                            Label = "Account email",
                            IsRequired = true
                        }
                    ]
                })));
        });
        var model = CreateModel(handler);
        model.OrderFields =
        [
            new DetailsModel.OrderFieldInput { FieldKey = "accountEmail", Value = "" }
        ];

        var result = await model.OnPostAsync(serviceId);

        Assert.IsType<PageResult>(result);
        var errors = model.ModelState["OrderFields[0].Value"]!.Errors;
        Assert.Contains(errors, x => x.ErrorMessage == "Account email is required.");
    }

    [Fact]
    public async Task OnPost_WhenApiReturnsUnauthorized_SignsOutAndRedirectsToLogin()
    {
        var authentication = new StubAuthenticationService();
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var model = CreateModel(handler, authentication);

        var result = await model.OnPostAsync(Guid.NewGuid());

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/Login", redirect.PageName);
        Assert.True(authentication.SignedOut);
    }

    private static DetailsModel CreateModel(
        HttpMessageHandler handler,
        IAuthenticationService? authentication = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(authentication ?? new StubAuthenticationService());
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, AppConstants.Roles.User)
            ], "Test"))
        };
        var model = new DetailsModel(new StubHttpClientFactory(handler))
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new StubTempDataProvider())
        };
        return model;
    }

    private static HttpResponseMessage JsonResponse<T>(HttpStatusCode status, T value)
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
        public bool SignedOut { get; private set; }

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
            AuthenticationProperties? properties)
        {
            SignedOut = true;
            return Task.CompletedTask;
        }
    }
}
