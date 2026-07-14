using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor.Services;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Web.Components.Pages;
using ServiceBridge.Web.Services;
using Xunit;
using OrdersPage = ServiceBridge.Web.Components.Pages.Orders;

namespace ServiceBridge.Tests.Web;

public sealed class OrderTrackingRealtimePageTests : BunitContext, IDisposable
{
    [Theory]
    [InlineData("Orders.razor")]
    [InlineData("PaymentSuccess.razor")]
    [InlineData("PaymentFailure.razor")]
    public void RealtimeSubscriptionOwnership_StaticArchitectureGuard(string pageName)
    {
        // Subscription ownership and disposal are structural conventions, not a single-render behavior.
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

    [Fact]
    public async Task Orders_RealtimeUpdate_ReloadsAndRendersLatestOrder()
    {
        var orderId = Guid.NewGuid();
        var handler = new OrderTrackingHttpHandler
        {
            OrdersResponse = call => Success(new PagedResultDto<UserOrderListItemDto>
            {
                Items = [OrderListItem(orderId, call == 1 ? "ORDER-INITIAL" : "ORDER-UPDATED")],
                Page = 1,
                PageSize = 20,
                TotalCount = 1
            })
        };
        var realtime = await ConfigureAsync(handler);

        var cut = Render<OrdersPage>();
        Assert.Contains("ORDER-INITIAL", cut.Markup);

        await realtime.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto { OrderId = orderId });

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("ORDER-UPDATED", cut.Markup);
            Assert.Equal(2, handler.OrdersCalls);
        });
    }

    [Fact]
    public async Task PaymentSuccess_RealtimeEvents_FilterByOrderAndRenderCurrentUpdate()
    {
        var orderId = Guid.NewGuid();
        var handler = new OrderTrackingHttpHandler
        {
            DetailResponse = call => Success(PaymentOrder(
                orderId,
                call == 1 ? "SUCCESS-INITIAL" : "SUCCESS-UPDATED",
                PaymentStatus.Completed))
        };
        var realtime = await ConfigureAsync(handler);

        var cut = Render<PaymentSuccess>(parameters => parameters
            .Add(component => component.OrderId, orderId));
        Assert.Contains("SUCCESS-INITIAL", cut.Markup);

        await realtime.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto { OrderId = Guid.NewGuid() });
        Assert.Equal(1, handler.DetailCalls);

        await realtime.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto { OrderId = orderId });
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("SUCCESS-UPDATED", cut.Markup);
            Assert.Equal(2, handler.DetailCalls);
        });
    }

    [Fact]
    public async Task PaymentFailure_RealtimeEvents_FilterByOrderAndRenderCurrentUpdate()
    {
        var orderId = Guid.NewGuid();
        var handler = new OrderTrackingHttpHandler
        {
            DetailResponse = call => Success(PaymentOrder(
                orderId,
                call == 1 ? "FAILURE-INITIAL" : "FAILURE-UPDATED",
                PaymentStatus.Failed))
        };
        var realtime = await ConfigureAsync(handler);

        var cut = Render<PaymentFailure>(parameters => parameters
            .Add(component => component.OrderId, orderId));
        Assert.Contains("FAILURE-INITIAL", cut.Markup);

        await realtime.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto { OrderId = Guid.NewGuid() });
        Assert.Equal(1, handler.DetailCalls);

        await realtime.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto { OrderId = orderId });
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("FAILURE-UPDATED", cut.Markup);
            Assert.Equal(2, handler.DetailCalls);
        });
    }

    private async Task<RealtimeHarness> ConfigureAsync(OrderTrackingHttpHandler handler)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices(options =>
            options.PopoverOptions.CheckForPopoverProvider = false);
        var authorization = AddAuthorization();
        authorization.SetAuthorized("Test user");

        var factory = new StubHttpClientFactory(handler);
        Services.AddSingleton<IHttpClientFactory>(factory);
        Services.AddSingleton(new UserOrdersClient(factory));
        Services.AddSingleton(new PaymentClient(factory));

        RealtimeConnectionHandlers? handlers = null;
        var realtimeClient = new RealtimeClient(
            NullLogger<RealtimeClient>.Instance,
            _ => Task.FromResult<string?>("test-token"),
            value =>
            {
                handlers = value;
                return new FakeRealtimeConnection();
            });
        await realtimeClient.StartAsync();
        Services.AddSingleton(realtimeClient);
        return new RealtimeHarness(handlers!);
    }

    void IDisposable.Dispose()
    {
        Services.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.Dispose();
    }

    private static UserOrderListItemDto OrderListItem(Guid id, string number) => new()
    {
        OrderId = id,
        OrderNumber = number,
        ServiceName = "Service",
        Status = nameof(OrderStatus.Processing),
        PaymentStatus = nameof(PaymentStatus.Completed),
        CreatedAt = DateTime.UtcNow
    };

    private static UserOrderDetailDto PaymentOrder(
        Guid id,
        string number,
        PaymentStatus paymentStatus) => new()
    {
        OrderId = id,
        OrderNumber = number,
        ServiceName = "Service",
        Provider = "Provider",
        FulfillmentMode = "Manual",
        Status = paymentStatus == PaymentStatus.Completed
            ? nameof(OrderStatus.Paid)
            : nameof(OrderStatus.WaitingForPayment),
        CreatedAt = DateTime.UtcNow,
        Payments =
        [
            new PaymentHistoryDto
            {
                PaymentId = Guid.NewGuid(),
                Status = paymentStatus.ToString(),
                CreatedAt = DateTime.UtcNow
            }
        ]
    };

    private static HttpResponseMessage Success<T>(T value) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new { success = true, data = value })
    };

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

    private sealed class OrderTrackingHttpHandler : HttpMessageHandler
    {
        public Func<int, HttpResponseMessage> OrdersResponse { get; init; } = _ =>
            Success(new PagedResultDto<UserOrderListItemDto>());
        public Func<int, HttpResponseMessage> DetailResponse { get; init; } = _ =>
            Success(new UserOrderDetailDto());
        public int OrdersCalls { get; private set; }
        public int DetailCalls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Get && path == "/api/orders/my")
            {
                return Task.FromResult(OrdersResponse(++OrdersCalls));
            }

            if (request.Method == HttpMethod.Get && path.StartsWith("/api/orders/my/", StringComparison.Ordinal))
            {
                return Task.FromResult(DetailResponse(++DetailCalls));
            }

            throw new InvalidOperationException($"Unexpected endpoint {request.Method} {path}");
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri("https://localhost/") };
    }

    private sealed record RealtimeHarness(RealtimeConnectionHandlers Handlers);

    private sealed class FakeRealtimeConnection : IRealtimeConnection
    {
        public HubConnectionState State { get; private set; } = HubConnectionState.Disconnected;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            State = HubConnectionState.Disconnected;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            State = HubConnectionState.Disconnected;
            return ValueTask.CompletedTask;
        }
    }
}
