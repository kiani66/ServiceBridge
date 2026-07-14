using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor.Services;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Web.Components.Pages;
using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public sealed class OrderDetailComponentConcurrencyTests : BunitContext, IDisposable
{
    [Fact]
    public async Task User_InitialLoadOverlappedByRealtime_DisplaysQueuedLatestResult()
    {
        var orderId = Guid.NewGuid();
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal();
        var handler = new OrderDetailHttpHandler(async (request, call, token) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == $"/api/orders/my/{orderId}")
            {
                if (call == 1)
                {
                    firstStarted.TrySetResult();
                    await releaseFirst.Task.WaitAsync(token);
                }

                return Success(UserOrder(orderId, call == 1 ? "USER-OLD" : "USER-LATEST"));
            }

            return EmptyRefunds(request);
        });
        var realtime = await ConfigureAsync(handler);

        var renderTask = Renderer.Dispatcher.InvokeAsync(() =>
            Render<OrderDetails>(parameters => parameters.Add(component => component.OrderId, orderId)));
        await firstStarted.Task;
        var realtimeTask = realtime.Handlers.OrderUpdated(
            new OrderUpdatedRealtimeDto { OrderId = orderId });
        releaseFirst.TrySetResult();

        var cut = await renderTask;
        await realtimeTask;
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("USER-LATEST", cut.Markup);
            Assert.DoesNotContain("USER-OLD", cut.Markup);
            Assert.Equal(2, handler.Calls($"GET /api/orders/my/{orderId}"));
        });
    }

    [Fact]
    public async Task Agent_InitialLoadOverlappedByRealtime_DisplaysQueuedLatestResult()
    {
        var orderId = Guid.NewGuid();
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal();
        var handler = new OrderDetailHttpHandler(async (request, call, token) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == $"/api/agent/orders/{orderId}")
            {
                if (call == 1)
                {
                    firstStarted.TrySetResult();
                    await releaseFirst.Task.WaitAsync(token);
                }

                return Success(AgentOrder(orderId, call == 1 ? "AGENT-OLD" : "AGENT-LATEST"));
            }

            throw Unexpected(request);
        });
        var realtime = await ConfigureAsync(handler);

        var renderTask = Renderer.Dispatcher.InvokeAsync(() =>
            Render<AgentOrderWorkspace>(parameters => parameters.Add(component => component.OrderId, orderId)));
        await firstStarted.Task;
        var realtimeTask = realtime.Handlers.OrderUpdated(
            new OrderUpdatedRealtimeDto { OrderId = orderId });
        releaseFirst.TrySetResult();

        var cut = await renderTask;
        await realtimeTask;
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("AGENT-LATEST", cut.Markup);
            Assert.DoesNotContain("AGENT-OLD", cut.Markup);
            Assert.Equal(2, handler.Calls($"GET /api/agent/orders/{orderId}"));
        });
    }

    [Fact]
    public async Task Admin_InitialLoadOverlappedByRealtime_DisplaysQueuedLatestResult()
    {
        var orderId = Guid.NewGuid();
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal();
        var handler = new OrderDetailHttpHandler(async (request, call, token) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == $"/api/admin/orders/{orderId}")
            {
                if (call == 1)
                {
                    firstStarted.TrySetResult();
                    await releaseFirst.Task.WaitAsync(token);
                }

                return Success(AdminOrder(orderId, call == 1 ? "ADMIN-OLD" : "ADMIN-LATEST"));
            }

            throw Unexpected(request);
        });
        var realtime = await ConfigureAsync(handler);

        var renderTask = Renderer.Dispatcher.InvokeAsync(() =>
            Render<AdminOrderManagement>(parameters => parameters.Add(component => component.OrderId, orderId)));
        await firstStarted.Task;
        var realtimeTask = realtime.Handlers.OrderUpdated(
            new OrderUpdatedRealtimeDto { OrderId = orderId });
        releaseFirst.TrySetResult();

        var cut = await renderTask;
        await realtimeTask;
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("ADMIN-LATEST", cut.Markup);
            Assert.DoesNotContain("ADMIN-OLD", cut.Markup);
            Assert.Equal(2, handler.Calls($"GET /api/admin/orders/{orderId}"));
        });
    }

    [Fact]
    public async Task Admin_MultipleRealtimeEvents_CollapseAndRecoverAfterActiveFailure()
    {
        var orderId = Guid.NewGuid();
        var activeStarted = NewSignal();
        var releaseActive = NewSignal();
        var handler = new OrderDetailHttpHandler(async (request, call, token) =>
        {
            if (request.Method != HttpMethod.Get ||
                request.RequestUri!.AbsolutePath != $"/api/admin/orders/{orderId}")
            {
                throw Unexpected(request);
            }

            if (call == 2)
            {
                activeStarted.TrySetResult();
                await releaseActive.Task.WaitAsync(token);
                return Failure("simulated refresh failure");
            }

            return Success(AdminOrder(orderId, call == 1 ? "ADMIN-INITIAL" : "ADMIN-RECOVERED"));
        });
        var realtime = await ConfigureAsync(handler);
        var cut = Render<AdminOrderManagement>(parameters =>
            parameters.Add(component => component.OrderId, orderId));

        var first = realtime.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto { OrderId = orderId });
        await activeStarted.Task;
        var second = realtime.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto { OrderId = orderId });
        var third = realtime.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto { OrderId = orderId });
        releaseActive.TrySetResult();
        await Task.WhenAll(first, second, third);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("ADMIN-RECOVERED", cut.Markup);
            Assert.Equal(3, handler.Calls($"GET /api/admin/orders/{orderId}"));
        });
    }

    [Fact]
    public async Task Admin_RouteChangesDuringActiveLoad_IgnoresPreviousOrderResult()
    {
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var staleStarted = NewSignal();
        var releaseStale = NewSignal();
        var handler = new OrderDetailHttpHandler(async (request, call, token) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == $"/api/admin/orders/{orderA}")
            {
                if (call == 2)
                {
                    staleStarted.TrySetResult();
                    await releaseStale.Task.WaitAsync(token);
                }

                return Success(AdminOrder(orderA, call == 1 ? "ORDER-A" : "ORDER-A-STALE"));
            }

            if (path == $"/api/admin/orders/{orderB}")
            {
                return Success(AdminOrder(orderB, "ORDER-B"));
            }

            throw Unexpected(request);
        });
        var realtime = await ConfigureAsync(handler);
        var cut = Render<AdminOrderManagement>(parameters =>
            parameters.Add(component => component.OrderId, orderA));

        var staleRefresh = realtime.Handlers.OrderUpdated(
            new OrderUpdatedRealtimeDto { OrderId = orderA });
        await staleStarted.Task;
        var routeChange = Renderer.Dispatcher.InvokeAsync(() =>
            cut.Render(parameters => parameters.Add(component => component.OrderId, orderB)));
        releaseStale.TrySetResult();
        await Task.WhenAll(staleRefresh, routeChange);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("ORDER-B", cut.Markup);
            Assert.DoesNotContain("ORDER-A-STALE", cut.Markup);
            Assert.Equal(1, handler.Calls($"GET /api/admin/orders/{orderB}"));
        });
    }

    [Fact]
    public async Task User_QueuedRouteChangeRejectsLatePreviousEvent()
    {
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var handler = new OrderDetailHttpHandler((request, _, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == $"/api/orders/my/{orderA}")
            {
                return Task.FromResult(Success(UserOrder(orderA, "USER-A")));
            }

            if (path == $"/api/orders/my/{orderB}")
            {
                return Task.FromResult(Success(UserOrder(orderB, "USER-B")));
            }

            return Task.FromResult(EmptyRefunds(request));
        });
        var realtime = await ConfigureAsync(handler);
        var cut = Render<OrderDetails>(parameters =>
            parameters.Add(component => component.OrderId, orderA));

        await using (var dispatcherBlock = await BlockRendererDispatcherAsync())
        {
            var routeChange = Renderer.Dispatcher.InvokeAsync(() =>
                cut.Render(parameters => parameters.Add(component => component.OrderId, orderB)));
            var staleEvent = realtime.Handlers.OrderUpdated(
                new OrderUpdatedRealtimeDto { OrderId = orderA });

            dispatcherBlock.Release();
            await Task.WhenAll(dispatcherBlock.Completion, routeChange, staleEvent);
        }

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("USER-B", cut.Markup);
            Assert.DoesNotContain("USER-A", cut.Markup);
            Assert.Equal(1, handler.Calls($"GET /api/orders/my/{orderA}"));
            Assert.Equal(1, handler.Calls($"GET /api/orders/my/{orderB}"));
        });
    }

    [Fact]
    public async Task Agent_QueuedRouteChangeRejectsLatePreviousEvent()
    {
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var handler = new OrderDetailHttpHandler((request, _, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == $"/api/agent/orders/{orderA}")
            {
                return Task.FromResult(Success(AgentOrder(orderA, "AGENT-A")));
            }

            if (path == $"/api/agent/orders/{orderB}")
            {
                return Task.FromResult(Success(AgentOrder(orderB, "AGENT-B")));
            }

            throw Unexpected(request);
        });
        var realtime = await ConfigureAsync(handler);
        var cut = Render<AgentOrderWorkspace>(parameters =>
            parameters.Add(component => component.OrderId, orderA));

        await using (var dispatcherBlock = await BlockRendererDispatcherAsync())
        {
            var routeChange = Renderer.Dispatcher.InvokeAsync(() =>
                cut.Render(parameters => parameters.Add(component => component.OrderId, orderB)));
            var staleEvent = realtime.Handlers.OrderUpdated(
                new OrderUpdatedRealtimeDto { OrderId = orderA });

            dispatcherBlock.Release();
            await Task.WhenAll(dispatcherBlock.Completion, routeChange, staleEvent);
        }

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("AGENT-B", cut.Markup);
            Assert.DoesNotContain("AGENT-A", cut.Markup);
            Assert.Equal(1, handler.Calls($"GET /api/agent/orders/{orderA}"));
            Assert.Equal(1, handler.Calls($"GET /api/agent/orders/{orderB}"));
        });
    }

    [Fact]
    public async Task Admin_QueuedRouteChangeRejectsLatePreviousEvent_AndCurrentEventsCoalesce()
    {
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var activeStarted = NewSignal();
        var releaseActive = NewSignal();
        var handler = new OrderDetailHttpHandler(async (request, call, token) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == $"/api/admin/orders/{orderA}")
            {
                return Success(AdminOrder(orderA, "ORDER-A"));
            }

            if (path == $"/api/admin/orders/{orderB}")
            {
                if (call == 2)
                {
                    activeStarted.TrySetResult();
                    await releaseActive.Task.WaitAsync(token);
                    return Success(AdminOrder(orderB, "ORDER-B-INTERMEDIATE"));
                }

                return Success(AdminOrder(
                    orderB,
                    call == 1 ? "ORDER-B-INITIAL" : "ORDER-B-LATEST"));
            }

            throw Unexpected(request);
        });
        var realtime = await ConfigureAsync(handler);
        var cut = Render<AdminOrderManagement>(parameters =>
            parameters.Add(component => component.OrderId, orderA));

        await using (var dispatcherBlock = await BlockRendererDispatcherAsync())
        {
            var routeChange = Renderer.Dispatcher.InvokeAsync(() =>
                cut.Render(parameters => parameters.Add(component => component.OrderId, orderB)));
            var staleEvent = realtime.Handlers.OrderUpdated(
                new OrderUpdatedRealtimeDto { OrderId = orderA });

            dispatcherBlock.Release();
            await Task.WhenAll(dispatcherBlock.Completion, routeChange, staleEvent);
        }

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("ORDER-B-INITIAL", cut.Markup);
            Assert.DoesNotContain("ORDER-A", cut.Markup);
            Assert.Equal(1, handler.Calls($"GET /api/admin/orders/{orderA}"));
            Assert.Equal(1, handler.Calls($"GET /api/admin/orders/{orderB}"));
        });

        var first = realtime.Handlers.OrderUpdated(
            new OrderUpdatedRealtimeDto { OrderId = orderB });
        await activeStarted.Task;
        var second = realtime.Handlers.OrderUpdated(
            new OrderUpdatedRealtimeDto { OrderId = orderB });
        var third = realtime.Handlers.OrderUpdated(
            new OrderUpdatedRealtimeDto { OrderId = orderB });
        releaseActive.TrySetResult();
        await Task.WhenAll(first, second, third);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("ORDER-B-LATEST", cut.Markup);
            Assert.DoesNotContain("ORDER-B-INTERMEDIATE", cut.Markup);
            Assert.Equal(3, handler.Calls($"GET /api/admin/orders/{orderB}"));
        });
    }

    [Fact]
    public async Task Admin_DisposeBeforeQueuedRealtimeCallback_PreventsMutationAndRendering()
    {
        var orderId = Guid.NewGuid();
        var handler = new OrderDetailHttpHandler((request, _, _) =>
        {
            if (request.Method == HttpMethod.Get &&
                request.RequestUri!.AbsolutePath == $"/api/admin/orders/{orderId}")
            {
                return Task.FromResult(Success(AdminOrder(orderId, "ADMIN-DISPOSE")));
            }

            throw Unexpected(request);
        });
        var realtime = await ConfigureAsync(handler);
        var cut = Render<AdminOrderManagement>(parameters =>
            parameters.Add(component => component.OrderId, orderId));
        var initialGeneration = GetPrivateField<long>(cut.Instance, "_loadGeneration");
        var initialRenderCount = cut.RenderCount;
        var initialRequestCount = handler.Calls($"GET /api/admin/orders/{orderId}");

        await using (var dispatcherBlock = await BlockRendererDispatcherAsync())
        {
            var callback = realtime.Handlers.OrderUpdated(
                new OrderUpdatedRealtimeDto { OrderId = orderId });
            cut.Instance.Dispose();

            dispatcherBlock.Release();
            await Task.WhenAll(dispatcherBlock.Completion, callback);
        }

        Assert.Equal(initialGeneration, GetPrivateField<long>(cut.Instance, "_loadGeneration"));
        Assert.Equal(initialRequestCount, handler.Calls($"GET /api/admin/orders/{orderId}"));
        Assert.Equal(initialRenderCount, cut.RenderCount);
        cut.Dispose();
    }

    [Fact]
    public async Task User_DisposeDuringPayment_PreventsNavigation()
    {
        var orderId = Guid.NewGuid();
        var actionStarted = NewSignal();
        var releaseAction = NewSignal();
        var handler = new OrderDetailHttpHandler(async (request, _, token) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == $"/api/orders/my/{orderId}")
            {
                return Success(UserOrder(orderId, "USER-PAY", OrderStatus.WaitingForPayment));
            }

            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/api/payments/initiate")
            {
                actionStarted.TrySetResult();
                await releaseAction.Task;
                return Success(new PaymentInitiatedResponse { PaymentId = Guid.NewGuid() });
            }

            return EmptyRefunds(request);
        });
        await ConfigureAsync(handler);
        var cut = Render<OrderDetails>(parameters => parameters.Add(component => component.OrderId, orderId));
        var pay = cut.FindAll("button")
            .Single(button => button.TextContent.Trim().EndsWith("Pay", StringComparison.Ordinal));

        var action = pay.ClickAsync(new MouseEventArgs());
        await actionStarted.Task;
        cut.Instance.Dispose();
        cut.Dispose();
        releaseAction.TrySetResult();
        await action;

        Assert.Empty(Services.GetRequiredService<BunitNavigationManager>().History);
    }

    [Fact]
    public async Task Agent_DisposeDuringAction_PreventsActionReload()
    {
        var orderId = Guid.NewGuid();
        var actionStarted = NewSignal();
        var releaseAction = NewSignal();
        var handler = new OrderDetailHttpHandler(async (request, _, token) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Get && path == $"/api/agent/orders/{orderId}")
            {
                return Success(AgentOrder(orderId, "AGENT-ACTION", OrderStatus.Assigned));
            }

            if (request.Method == HttpMethod.Post && path == $"/api/agent/orders/{orderId}/start")
            {
                actionStarted.TrySetResult();
                await releaseAction.Task;
                return CommandSuccess();
            }

            throw Unexpected(request);
        });
        await ConfigureAsync(handler);
        var cut = Render<AgentOrderWorkspace>(parameters => parameters.Add(component => component.OrderId, orderId));
        var initialGets = handler.Calls($"GET /api/agent/orders/{orderId}");
        var start = cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Start processing", StringComparison.Ordinal));

        var action = start.ClickAsync(new MouseEventArgs());
        await actionStarted.Task;
        cut.Instance.Dispose();
        cut.Dispose();
        releaseAction.TrySetResult();
        await action;

        Assert.Equal(initialGets, handler.Calls($"GET /api/agent/orders/{orderId}"));
    }

    [Fact]
    public async Task Admin_DisposeDuringCredentialReveal_DoesNotExposeSensitiveValue()
    {
        var orderId = Guid.NewGuid();
        var revealStarted = NewSignal();
        var releaseReveal = NewSignal();
        var handler = new OrderDetailHttpHandler(async (request, _, token) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Get && path == $"/api/admin/orders/{orderId}")
            {
                var order = AdminOrder(orderId, "ADMIN-REVEAL");
                order.Credentials =
                [
                    new AgentOrderFieldDto
                    {
                        FieldKey = "password",
                        FieldLabel = "Password",
                        Value = string.Empty,
                        IsSensitive = true
                    }
                ];
                return Success(order);
            }

            if (request.Method == HttpMethod.Post && path == $"/api/admin/orders/{orderId}/credentials/reveal")
            {
                revealStarted.TrySetResult();
                await releaseReveal.Task;
                return Success(new AdminOrderCredentialsDto
                {
                    OrderId = orderId,
                    Credentials =
                    [
                        new AgentOrderFieldDto
                        {
                            FieldKey = "password",
                            FieldLabel = "Password",
                            Value = "top-secret",
                            IsSensitive = true
                        }
                    ]
                });
            }

            throw Unexpected(request);
        });
        await ConfigureAsync(handler);
        var cut = Render<AdminOrderManagement>(parameters =>
            parameters.Add(component => component.OrderId, orderId));
        var component = cut.Instance;
        var reveal = cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Reveal credentials", StringComparison.Ordinal));

        var action = reveal.ClickAsync(new MouseEventArgs());
        await revealStarted.Task;
        component.Dispose();
        cut.Dispose();
        releaseReveal.TrySetResult();
        await action;

        var revealed = Assert.IsAssignableFrom<IEnumerable>(
            typeof(AdminOrderManagement)
                .GetField("_revealedCredentialItems", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(component));
        Assert.Empty(revealed.Cast<object>());
        Assert.Empty(Services.GetRequiredService<BunitNavigationManager>().History);
    }

    private async Task<RealtimeHarness> ConfigureAsync(OrderDetailHttpHandler handler)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices(options => options.PopoverOptions.CheckForPopoverProvider = false);
        var factory = new StubHttpClientFactory(handler);
        Services.AddSingleton<IHttpClientFactory>(factory);
        Services.AddSingleton(new UserOrdersClient(factory));
        Services.AddSingleton(new RefundsClient(factory));
        Services.AddSingleton(new PaymentClient(factory));
        Services.AddSingleton(new AgentOrdersClient(factory));
        Services.AddSingleton(new AdminOrdersClient(factory));

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

    private static UserOrderDetailDto UserOrder(
        Guid id,
        string number,
        OrderStatus status = OrderStatus.Processing) => new()
    {
        OrderId = id,
        OrderNumber = number,
        ServiceName = "Service",
        Provider = "Provider",
        FulfillmentMode = "Manual",
        Status = status.ToString(),
        CreatedAt = DateTime.UtcNow
    };

    private static AgentOrderDetailDto AgentOrder(
        Guid id,
        string number,
        OrderStatus status = OrderStatus.Processing) => new()
    {
        OrderId = id,
        OrderNumber = number,
        ServiceName = "Service",
        Provider = "Provider",
        CustomerName = "Customer",
        CustomerEmail = "customer@example.com",
        FulfillmentMode = "Manual",
        Status = status.ToString(),
        CreatedAt = DateTime.UtcNow
    };

    private static AdminOrderDetailDto AdminOrder(Guid id, string number) => new()
    {
        OrderId = id,
        OrderNumber = number,
        CustomerName = "Customer",
        CustomerEmail = "customer@example.com",
        ServiceName = "Service",
        Provider = "Provider",
        FulfillmentMode = "Manual",
        Status = OrderStatus.Processing.ToString(),
        CreatedAt = DateTime.UtcNow
    };

    private static HttpResponseMessage EmptyRefunds(HttpRequestMessage request)
    {
        if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath.EndsWith("/refunds", StringComparison.Ordinal))
        {
            return Success(new PagedResultDto<RefundRequestDto>
            {
                Items = Array.Empty<RefundRequestDto>(),
                Page = 1,
                PageSize = 20,
                TotalCount = 0
            });
        }

        throw Unexpected(request);
    }

    private static HttpResponseMessage Success<T>(T data) => JsonResponse(
        HttpStatusCode.OK,
        new { success = true, data });

    private static HttpResponseMessage CommandSuccess() => JsonResponse(
        HttpStatusCode.OK,
        new { success = true, message = "ok" });

    private static HttpResponseMessage Failure(string message) => JsonResponse(
        HttpStatusCode.Conflict,
        new { success = false, message });

    private static HttpResponseMessage JsonResponse<T>(HttpStatusCode status, T value) =>
        new(status) { Content = JsonContent.Create(value) };

    private static InvalidOperationException Unexpected(HttpRequestMessage request) =>
        new($"Unexpected endpoint {request.Method} {request.RequestUri!.PathAndQuery}");

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private async Task<RendererDispatcherBlock> BlockRendererDispatcherAsync()
    {
        var entered = NewSignal();
        var release = new ManualResetEventSlim();
        var completion = Task.Run(() => Renderer.Dispatcher.InvokeAsync(() =>
        {
            entered.TrySetResult();
            release.Wait();
        }));

        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        return new RendererDispatcherBlock(release, completion);
    }

    private static T GetPrivateField<T>(object instance, string name) =>
        (T)instance.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance)!;

    private sealed class OrderDetailHttpHandler(
        Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> response)
        : HttpMessageHandler
    {
        private readonly ConcurrentDictionary<string, int> _calls = new();

        public int Calls(string key) => _calls.GetValueOrDefault(key);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            var call = _calls.AddOrUpdate(key, 1, (_, current) => current + 1);
            return response(request, call, cancellationToken);
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

    private sealed class RendererDispatcherBlock(
        ManualResetEventSlim release,
        Task completion) : IAsyncDisposable
    {
        private int _released;

        public Task Completion { get; } = completion;

        public void Release()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                release.Set();
            }
        }

        public async ValueTask DisposeAsync()
        {
            Release();
            await Completion;
            release.Dispose();
        }
    }

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
