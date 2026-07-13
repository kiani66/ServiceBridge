using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor.Services;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Web.Services;
using Xunit;
using AgentOrdersPage = ServiceBridge.Web.Components.Pages.AgentOrders;

namespace ServiceBridge.Tests.Web;

public sealed class AgentOrdersComponentLifecycleTests : BunitContext, IDisposable
{
    [Fact]
    public async Task InitialRender_LoadsExactlyOnce()
    {
        var handler = new AgentOrdersHttpHandler((request, _, _) =>
            Task.FromResult(OrdersResponse(request, "INITIAL")));
        await ConfigureAsync(handler);

        var cut = Render<AgentOrdersPage>();

        cut.WaitForAssertion(() => Assert.Contains("INITIAL", cut.Markup));
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("Paid", null, null, "status=Paid")]
    [InlineData(null, "customer", null, "search=customer")]
    [InlineData(null, null, "2", "page=2")]
    public async Task QueryChange_LoadsExactlyOnce(
        string? status,
        string? search,
        string? page,
        string expectedQuery)
    {
        var handler = new AgentOrdersHttpHandler((request, call, _) =>
            Task.FromResult(OrdersResponse(request, $"REQUEST-{call}")));
        await ConfigureAsync(handler);
        var cut = Render<AgentOrdersPage>();
        var navigation = Services.GetRequiredService<BunitNavigationManager>();

        navigation.NavigateTo(QueryUri(status, search, page));

        cut.WaitForAssertion(() => Assert.Contains("REQUEST-2", cut.Markup));
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains(expectedQuery, handler.Requests.Last(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReselectingCurrentQuery_DoesNotLoadOrNavigate()
    {
        var handler = new AgentOrdersHttpHandler((request, _, _) =>
            Task.FromResult(OrdersResponse(request, "CURRENT")));
        await ConfigureAsync(handler);
        var navigation = Services.GetRequiredService<BunitNavigationManager>();
        navigation.NavigateTo(QueryUri("Processing", "customer", "2"));
        var cut = Render<AgentOrdersPage>();
        var historyCount = navigation.History.Count;

        cut.Find("input[aria-label='Search order, customer, service']").Input("customer");

        Assert.Single(handler.Requests);
        Assert.Equal(historyCount, navigation.History.Count);
    }

    [Fact]
    public async Task BrowserQueryTransitions_LoadEachDistinctStateOnce()
    {
        var handler = new AgentOrdersHttpHandler((request, call, _) =>
            Task.FromResult(OrdersResponse(request, $"STATE-{call}")));
        await ConfigureAsync(handler);
        var cut = Render<AgentOrdersPage>();
        var navigation = Services.GetRequiredService<BunitNavigationManager>();

        navigation.NavigateTo(QueryUri(null, null, "2"));
        navigation.NavigateTo(QueryUri(null, null, null));

        cut.WaitForAssertion(() => Assert.Contains("STATE-3", cut.Markup));
        Assert.Equal(3, handler.Requests.Count);
        Assert.Contains("page=1", handler.Requests.ElementAt(0), StringComparison.Ordinal);
        Assert.Contains("page=2", handler.Requests.ElementAt(1), StringComparison.Ordinal);
        Assert.Contains("page=1", handler.Requests.ElementAt(2), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OlderBlockedRefresh_CannotOverwriteNewerQueryResult()
    {
        var staleStarted = NewSignal();
        var releaseStale = NewSignal();
        var handler = new AgentOrdersHttpHandler(async (request, call, token) =>
        {
            if (call == 2)
            {
                staleStarted.TrySetResult();
                await releaseStale.Task.WaitAsync(token);
                return OrdersResponse(request, "STALE");
            }

            return OrdersResponse(request, call == 1 ? "INITIAL" : "LATEST");
        });
        var realtime = await ConfigureAsync(handler);
        var cut = Render<AgentOrdersPage>();
        var navigation = Services.GetRequiredService<BunitNavigationManager>();

        var staleRefresh = realtime.Handlers.OrderUpdated(
            new OrderUpdatedRealtimeDto { OrderId = Guid.NewGuid() });
        await staleStarted.Task;
        navigation.NavigateTo(QueryUri("Paid", null, null));
        cut.WaitForAssertion(() => Assert.Equal(3, handler.Requests.Count));
        releaseStale.TrySetResult();
        await staleRefresh;

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("LATEST", cut.Markup);
            Assert.DoesNotContain("STALE", cut.Markup);
            Assert.Equal(3, handler.Requests.Count);
        });
    }

    [Theory]
    [InlineData("invalid", "abc")]
    [InlineData("999", "0")]
    [InlineData("", "-1")]
    [InlineData(null, "999999999999999999999999999999999999")]
    public async Task InvalidStatusAndPage_LoadWithoutFilterOnPageOne(
        string? status,
        string? page)
    {
        var handler = new AgentOrdersHttpHandler((request, _, _) =>
            Task.FromResult(OrdersResponse(request, "SAFE")));
        await ConfigureAsync(handler);
        var navigation = Services.GetRequiredService<BunitNavigationManager>();
        navigation.NavigateTo(QueryUri(status, null, page));

        var cut = Render<AgentOrdersPage>();

        cut.WaitForAssertion(() => Assert.Contains("SAFE", cut.Markup));
        var request = Assert.Single(handler.Requests);
        Assert.Contains("page=1", request, StringComparison.Ordinal);
        Assert.DoesNotContain("status=", request, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NextPageNavigation_PreservesStatusAndSearchThenLoadsOnce()
    {
        var handler = new AgentOrdersHttpHandler((request, call, _) =>
            Task.FromResult(OrdersResponse(request, $"PAGE-{call}", totalCount: 60)));
        await ConfigureAsync(handler);
        var navigation = Services.GetRequiredService<BunitNavigationManager>();
        navigation.NavigateTo(QueryUri("Processing", "customer", null));
        var cut = Render<AgentOrdersPage>();

        var next = cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Next", StringComparison.Ordinal));
        await next.ClickAsync(new MouseEventArgs());

        Assert.Contains("page=2", navigation.Uri, StringComparison.Ordinal);
        Assert.Contains("status=Processing", navigation.Uri, StringComparison.Ordinal);
        Assert.Contains("search=customer", navigation.Uri, StringComparison.Ordinal);

        cut.WaitForAssertion(() => Assert.Contains("PAGE-2", cut.Markup));
        Assert.Equal(2, handler.Requests.Count);
    }

    private async Task<RealtimeHarness> ConfigureAsync(AgentOrdersHttpHandler handler)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices(options => options.PopoverOptions.CheckForPopoverProvider = false);
        var factory = new StubHttpClientFactory(handler);
        Services.AddSingleton<IHttpClientFactory>(factory);
        Services.AddSingleton(new AgentOrdersClient(factory));

        RealtimeConnectionHandlers? handlers = null;
        var client = new RealtimeClient(
            NullLogger<RealtimeClient>.Instance,
            _ => Task.FromResult<string?>("test-token"),
            value =>
            {
                handlers = value;
                return new FakeRealtimeConnection();
            });
        await client.StartAsync();
        Services.AddSingleton(client);
        return new RealtimeHarness(client, handlers!);
    }

    void IDisposable.Dispose()
    {
        Services.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.Dispose();
    }

    private static HttpResponseMessage OrdersResponse(
        HttpRequestMessage request,
        string orderNumber,
        int totalCount = 1)
    {
        var page = request.RequestUri!.Query.Contains("page=2", StringComparison.Ordinal) ? 2 : 1;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                success = true,
                data = new PagedResultDto<AgentOrderListItemDto>
                {
                    Items =
                    [
                        new AgentOrderListItemDto
                        {
                            OrderId = Guid.NewGuid(),
                            OrderNumber = orderNumber,
                            CustomerName = "Customer",
                            ServiceName = "Service",
                            Status = OrderStatus.Processing.ToString(),
                            CreatedAt = DateTime.UtcNow
                        }
                    ],
                    Page = page,
                    PageSize = 20,
                    TotalCount = totalCount
                }
            })
        };
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static string QueryUri(string? status, string? search, string? page)
    {
        var values = new List<string>();
        if (status is not null)
        {
            values.Add($"status={Uri.EscapeDataString(status)}");
        }

        if (search is not null)
        {
            values.Add($"search={Uri.EscapeDataString(search)}");
        }

        if (page is not null)
        {
            values.Add($"page={Uri.EscapeDataString(page)}");
        }

        return values.Count == 0
            ? "/ui/agent/orders"
            : $"/ui/agent/orders?{string.Join('&', values)}";
    }

    private sealed class AgentOrdersHttpHandler(
        Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> response)
        : HttpMessageHandler
    {
        private int _calls;
        public ConcurrentQueue<string> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Enqueue(request.RequestUri!.PathAndQuery);
            return response(request, Interlocked.Increment(ref _calls), cancellationToken);
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri("https://localhost/") };
    }

    private sealed record RealtimeHarness(
        RealtimeClient Client,
        RealtimeConnectionHandlers Handlers);

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
