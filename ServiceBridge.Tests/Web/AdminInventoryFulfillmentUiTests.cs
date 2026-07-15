using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor.Services;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Web.Components.Pages;
using ServiceBridge.Web.Services;
using Xunit;
using AdminOrdersPage = ServiceBridge.Web.Components.Pages.AdminOrders;

namespace ServiceBridge.Tests.Web;

public sealed class AdminInventoryFulfillmentUiTests : BunitContext, IDisposable
{
    [Fact]
    public async Task PendingQueue_RendersSafeFieldsAndGuardsDoubleRetrySubmission()
    {
        var orderId = Guid.NewGuid();
        var postStarted = Signal();
        var releasePost = Signal();
        var handler = new Handler(async (request, _, token) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Get && path == "/api/admin/orders/paid-unassigned")
            {
                return Success(new PagedResultDto<AdminAssignableOrderDto>());
            }

            if (request.Method == HttpMethod.Get && path == "/api/admin/orders/agents")
            {
                return Success<IReadOnlyCollection<AgentSummaryDto>>(Array.Empty<AgentSummaryDto>());
            }

            if (request.Method == HttpMethod.Get && path == "/api/admin/orders/inventory-fulfillment/pending")
            {
                Assert.Equal("?page=1&pageSize=20", request.RequestUri.Query);
                return Success(PendingPage(orderId));
            }

            if (request.Method == HttpMethod.Post &&
                path == $"/api/admin/orders/{orderId}/inventory-fulfillment/retry")
            {
                postStarted.TrySetResult();
                await releasePost.Task.WaitAsync(token);
                return Success(new RetryInventoryCodeFulfillmentResultDto
                {
                    OrderId = orderId,
                    Outcome = InventoryCodeFulfillmentOutcome.NoInventory
                });
            }

            throw Unexpected(request);
        });
        await ConfigureAsync(handler);

        var cut = Render<AdminOrdersPage>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Awaiting stock", cut.Markup);
            Assert.Contains("Recoverable allocation", cut.Markup);
            Assert.Contains("Inconsistent", cut.Markup);
            Assert.Equal(2, cut.FindAll("button").Count(button =>
                button.TextContent.Contains("Retry", StringComparison.Ordinal)));
            Assert.DoesNotContain("ProtectedCode", cut.Markup);
            Assert.DoesNotContain("CodeFingerprint", cut.Markup);
            Assert.DoesNotContain("credential", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        await cut.FindAll("button").First(button =>
            button.TextContent.Trim() == "Retry").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        var confirm = cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Retry fulfillment", StringComparison.Ordinal));
        var first = confirm.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await postStarted.Task;
        cut.WaitForAssertion(() => Assert.True(cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Retrying", StringComparison.Ordinal)).HasAttribute("disabled")));
        releasePost.TrySetResult();
        await first;

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, handler.Calls($"POST /api/admin/orders/{orderId}/inventory-fulfillment/retry"));
            Assert.Contains("No inventory is currently available", cut.Markup);
            Assert.Contains("OPS-AWAIT", cut.Markup);
        });
    }

    [Fact]
    public async Task Detail_ShowsInventorySectionOnlyForInventoryCodeAndNoSecretFields()
    {
        var inventoryOrderId = Guid.NewGuid();
        var manualOrderId = Guid.NewGuid();
        var handler = new Handler((request, _, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Get && path == $"/api/admin/orders/{inventoryOrderId}")
            {
                return Task.FromResult(Success(Detail(inventoryOrderId, "InventoryCode")));
            }

            if (request.Method == HttpMethod.Get && path == $"/api/admin/orders/{manualOrderId}")
            {
                return Task.FromResult(Success(Detail(manualOrderId, "ManualAgent")));
            }

            throw Unexpected(request);
        });
        await ConfigureAsync(handler);

        var cut = Render<AdminOrderManagement>(parameters => parameters
            .Add(component => component.OrderId, inventoryOrderId));
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Inventory fulfillment", cut.Markup);
            Assert.Contains("Recoverable allocation", cut.Markup);
            Assert.Contains("Retry fulfillment", cut.Markup);
            Assert.DoesNotContain("ProtectedCode", cut.Markup);
            Assert.DoesNotContain("CodeFingerprint", cut.Markup);
        });

        await Renderer.Dispatcher.InvokeAsync(() => cut.Render(parameters =>
            parameters.Add(component => component.OrderId, manualOrderId)));
        cut.WaitForAssertion(() => Assert.DoesNotContain("Inventory fulfillment", cut.Markup));
    }

    [Fact]
    public void UiPath_UsesServerRetryEligibilityAndBoundedOrderUpdatedRefresh()
    {
        var list = File.ReadAllText(WebPath("Components", "Pages", "AdminOrders.razor"));
        var detail = File.ReadAllText(WebPath("Components", "Pages", "AdminOrderManagement.razor"));

        Assert.Contains("context.CanRetryInventoryFulfillment", list);
        Assert.Contains("_order.CanRetryInventoryFulfillment", detail);
        Assert.Contains("RetryInventoryCodeFulfillmentAsync", list);
        Assert.Contains("RetryInventoryCodeFulfillmentAsync", detail);
        Assert.Contains("if (_disposed || _retryBusy", list);
        Assert.Contains("if (_disposed || _retryBusy", detail);
        Assert.Contains("SubscribeToOrderUpdates", list);
        Assert.Contains("SubscribeToOrderUpdates", detail);
        Assert.Contains("RealtimeRefreshCoordinator", list);
        Assert.Contains("RealtimeRefreshCoordinator", detail);
        Assert.DoesNotContain("ProtectedCode", list);
        Assert.DoesNotContain("CodeFingerprint", list);
    }

    private async Task ConfigureAsync(Handler handler)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices(options => options.PopoverOptions.CheckForPopoverProvider = false);
        var factory = new Factory(handler);
        Services.AddSingleton<IHttpClientFactory>(factory);
        Services.AddSingleton(new AdminOrdersClient(factory));

        RealtimeConnectionHandlers? handlers = null;
        var realtime = new RealtimeClient(
            NullLogger<RealtimeClient>.Instance,
            _ => Task.FromResult<string?>("test-token"),
            value =>
            {
                handlers = value;
                return new Connection();
            });
        await realtime.StartAsync();
        Assert.NotNull(handlers);
        Services.AddSingleton(realtime);
    }

    private static PagedResultDto<AdminPendingInventoryFulfillmentDto> PendingPage(Guid orderId) => new()
    {
        Page = 1,
        PageSize = 20,
        TotalCount = 3,
        Items =
        [
            new AdminPendingInventoryFulfillmentDto
            {
                OrderId = orderId, OrderNumber = "OPS-AWAIT", UserName = "Customer",
                ServiceName = "Service", VariantName = "Variant", OrderStatus = nameof(OrderStatus.Paid),
                InventoryFulfillmentState = AdminInventoryFulfillmentState.AwaitingStock,
                CanRetryInventoryFulfillment = true, CreatedAt = DateTime.UtcNow
            },
            new AdminPendingInventoryFulfillmentDto
            {
                OrderId = Guid.NewGuid(), OrderNumber = "OPS-RESERVED", UserName = "Customer",
                ServiceName = "Service", OrderStatus = nameof(OrderStatus.Processing),
                InventoryFulfillmentState = AdminInventoryFulfillmentState.RecoverableAllocation,
                InventoryAllocationStatus = nameof(InventoryItemStatus.Reserved),
                CanRetryInventoryFulfillment = true, CreatedAt = DateTime.UtcNow
            },
            new AdminPendingInventoryFulfillmentDto
            {
                OrderId = Guid.NewGuid(), OrderNumber = "OPS-INCONSISTENT", UserName = "Customer",
                ServiceName = "Service", OrderStatus = nameof(OrderStatus.Paid),
                InventoryFulfillmentState = AdminInventoryFulfillmentState.Inconsistent,
                InventoryAllocationStatus = nameof(InventoryItemStatus.Delivered),
                CreatedAt = DateTime.UtcNow
            }
        ]
    };

    private static AdminOrderDetailDto Detail(Guid orderId, string mode) => new()
    {
        OrderId = orderId, OrderNumber = "OPS-DETAIL", CustomerName = "Customer",
        CustomerEmail = "customer@example.test", ServiceName = "Service", Provider = "Provider",
        FulfillmentMode = "Existing", ServiceFulfillmentMode = mode,
        Status = nameof(OrderStatus.Processing), CreatedAt = DateTime.UtcNow,
        InventoryFulfillmentState = AdminInventoryFulfillmentState.RecoverableAllocation,
        InventoryAllocationStatus = nameof(InventoryItemStatus.Reserved),
        CanRetryInventoryFulfillment = mode == "InventoryCode"
    };

    private static HttpResponseMessage Success<T>(T data) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new { success = true, data })
    };

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static InvalidOperationException Unexpected(HttpRequestMessage request) => new($"Unexpected endpoint {request.Method} {request.RequestUri}");
    private static string WebPath(params string[] parts) => Path.Combine([Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")), "ServiceBridge.Web", .. parts]);

    void IDisposable.Dispose()
    {
        Services.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.Dispose();
    }

    private sealed class Handler(Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        private readonly ConcurrentDictionary<string, int> _calls = new();
        public int Calls(string key) => _calls.GetValueOrDefault(key);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            return send(request, _calls.AddOrUpdate(key, 1, (_, current) => current + 1), cancellationToken);
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false) { BaseAddress = new Uri("https://localhost/") };
    }

    private sealed class Connection : IRealtimeConnection
    {
        public HubConnectionState State { get; private set; } = HubConnectionState.Disconnected;
        public Task StartAsync(CancellationToken cancellationToken) { State = HubConnectionState.Connected; return Task.CompletedTask; }
        public Task StopAsync(CancellationToken cancellationToken) { State = HubConnectionState.Disconnected; return Task.CompletedTask; }
        public ValueTask DisposeAsync() { State = HubConnectionState.Disconnected; return ValueTask.CompletedTask; }
    }
}
