using System.Net;
using System.Net.Http.Json;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Web.Components.Navigation;
using ServiceBridge.Web.Components.Pages;
using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public sealed class NotificationComponentConcurrencyTests : BunitContext, IDisposable
{
    [Fact]
    public async Task Bell_OpenDuringStaleRefresh_EndsWithAuthoritativeReadState()
    {
        var notificationId = Guid.NewGuid();
        var releaseStale = NewSignal();
        var staleStarted = NewSignal();
        var handler = new NotificationHttpHandler
        {
            CountResponse = async (call, token) =>
            {
                if (call == 2)
                {
                    staleStarted.TrySetResult();
                    await releaseStale.Task.WaitAsync(token);
                }

                return CountResponse(call < 3 ? 1 : 0);
            },
            ListResponse = (call, _) => Task.FromResult(
                ListResponse(Notification(notificationId, isRead: call >= 3))),
            MarkResponse = (_, _) => Task.FromResult(CommandResponse(success: true))
        };
        var realtime = await ConfigureAsync(handler);

        var cut = Render<NotificationBell>();
        await EnsureBellMenuOpenAsync(realtime.Popover, cut);
        var staleRefresh = realtime.Handlers.NotificationReceived(new RealtimeEventDto());
        await staleStarted.Task;
        await realtime.Popover.Find("button[data-notification-action='open']")
            .ClickAsync(new MouseEventArgs());
        releaseStale.TrySetResult();
        await staleRefresh;
        await EnsureBellMenuOpenAsync(realtime.Popover, cut);

        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.FindComponent<MudBadge>().Instance.Visible);
            Assert.Equal(3, handler.CountCalls);
            Assert.Equal(3, handler.ListCalls);
            Assert.Equal(1, handler.MarkCalls);
            Assert.True(realtime.Popover.Find("button[data-notification-action='open']")
                .ClassList.Contains("sb-notification-read"));
        });
        Assert.Single(Services.GetRequiredService<BunitNavigationManager>().History);
    }

    [Fact]
    public async Task Page_MarkAllDuringStaleRefresh_DoesNotRestoreUnreadState()
    {
        var notificationId = Guid.NewGuid();
        var releaseStale = NewSignal();
        var staleStarted = NewSignal();
        var handler = new NotificationHttpHandler
        {
            ListResponse = async (call, token) =>
            {
                if (call == 2)
                {
                    staleStarted.TrySetResult();
                    await releaseStale.Task.WaitAsync(token);
                }

                return ListResponse(Notification(notificationId, isRead: call >= 3));
            },
            CountResponse = (call, _) => Task.FromResult(CountResponse(call < 3 ? 1 : 0)),
            MarkAllResponse = (_, _) => Task.FromResult(CommandResponse(success: true))
        };
        var realtime = await ConfigureAsync(handler);

        var cut = Render<Notifications>();
        var markAll = cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Mark all read", StringComparison.Ordinal));

        var staleRefresh = realtime.Handlers.NotificationReceived(new RealtimeEventDto());
        await staleStarted.Task;
        markAll.Click();
        releaseStale.TrySetResult();
        await staleRefresh;

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("0 unread", cut.Markup);
            Assert.Contains(">Read<", cut.Markup);
            Assert.DoesNotContain(">Unread<", cut.Markup);
            Assert.Equal(3, handler.ListCalls);
            Assert.Equal(3, handler.CountCalls);
            Assert.Equal(1, handler.MarkAllCalls);
        });
    }

    [Fact]
    public async Task Bell_RapidDuplicateOpen_SendsOneMarkAndNavigatesOnce()
    {
        var notificationId = Guid.NewGuid();
        var markStarted = NewSignal();
        var releaseMark = NewSignal();
        var handler = new NotificationHttpHandler
        {
            CountResponse = (call, _) => Task.FromResult(CountResponse(call == 1 ? 1 : 0)),
            ListResponse = (call, _) => Task.FromResult(
                ListResponse(Notification(notificationId, isRead: call > 1))),
            MarkResponse = async (_, token) =>
            {
                markStarted.TrySetResult();
                await releaseMark.Task.WaitAsync(token);
                return CommandResponse(success: true);
            }
        };
        var realtime = await ConfigureAsync(handler);
        var cut = Render<NotificationBell>();
        await EnsureBellMenuOpenAsync(realtime.Popover, cut);
        var openAction = realtime.Popover.Find("button[data-notification-action='open']");
        var first = openAction.ClickAsync(new MouseEventArgs());
        await markStarted.Task;
        Assert.Empty(realtime.Popover.FindAll("button[data-notification-action='open']"));

        await EnsureBellMenuOpenAsync(realtime.Popover, cut);
        var second = realtime.Popover.Find("button[data-notification-action='open']")
            .ClickAsync(new MouseEventArgs());
        releaseMark.TrySetResult();
        await Task.WhenAll(first, second);

        if (realtime.Popover.FindAll("button[data-notification-action='open']").Count > 0)
        {
            await cut.InvokeAsync(() => cut.Find("button[aria-label='Notifications']").Click());
        }

        realtime.Popover.WaitForAssertion(() =>
            Assert.Empty(realtime.Popover.FindAll("button[data-notification-action='open']")));
        await EnsureBellMenuOpenAsync(realtime.Popover, cut);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, handler.MarkCalls);
            var navigation = Services.GetRequiredService<BunitNavigationManager>();
            Assert.Single(navigation.History);
            Assert.EndsWith(
                "/ui/orders/00000000-0000-0000-0000-000000000001",
                navigation.Uri,
                StringComparison.Ordinal);
            Assert.Equal(2, handler.CountCalls);
            Assert.Equal(2, handler.ListCalls);
            var badge = cut.FindComponent<MudBadge>().Instance;
            Assert.False(badge.Visible);
            Assert.Equal("0", badge.Content?.ToString());
            Assert.True(realtime.Popover.Find("button[data-notification-action='open']")
                .ClassList.Contains("sb-notification-read"));
        });
    }

    [Fact]
    public async Task Bell_MarkFailureStillNavigates_AndLaterRefreshRecovers()
    {
        var notificationId = Guid.NewGuid();
        var handler = new NotificationHttpHandler
        {
            CountResponse = (call, _) => Task.FromResult(CountResponse(call < 3 ? 1 : 0)),
            ListResponse = (call, _) => Task.FromResult(
                ListResponse(Notification(notificationId, isRead: call >= 3))),
            MarkResponse = (_, _) => Task.FromResult(CommandResponse(success: false))
        };
        var realtime = await ConfigureAsync(handler);
        var cut = Render<NotificationBell>();
        await EnsureBellMenuOpenAsync(realtime.Popover, cut);
        await realtime.Popover.Find("button[data-notification-action='open']")
            .ClickAsync(new MouseEventArgs());
        await EnsureBellMenuOpenAsync(realtime.Popover, cut);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, handler.CountCalls);
            Assert.Equal(2, handler.ListCalls);
            Assert.Single(Services.GetRequiredService<BunitNavigationManager>().History);
            Assert.False(realtime.Popover.Find("button[data-notification-action='open']")
                .ClassList.Contains("sb-notification-read"));
        });

        await realtime.Handlers.NotificationReceived(new RealtimeEventDto());
        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.FindComponent<MudBadge>().Instance.Visible);
            Assert.Equal(3, handler.CountCalls);
            Assert.Equal(3, handler.ListCalls);
            Assert.True(realtime.Popover.Find("button[data-notification-action='open']")
                .ClassList.Contains("sb-notification-read"));
        });
    }

    [Fact]
    public async Task Bell_DisposeWithPendingRefresh_DiscardsPendingWork()
    {
        var notificationId = Guid.NewGuid();
        var staleStarted = NewSignal();
        var releaseStale = NewSignal();
        var handler = new NotificationHttpHandler
        {
            CountResponse = async (call, token) =>
            {
                if (call == 2)
                {
                    staleStarted.TrySetResult();
                    await releaseStale.Task.WaitAsync(token);
                }

                return CountResponse(1);
            },
            ListResponse = (_, _) => Task.FromResult(
                ListResponse(Notification(notificationId, isRead: false))),
            MarkResponse = (_, _) => Task.FromResult(CommandResponse(success: true))
        };
        var realtime = await ConfigureAsync(handler);
        var cut = Render<NotificationBell>();
        await EnsureBellMenuOpenAsync(realtime.Popover, cut);

        var staleRefresh = realtime.Handlers.NotificationReceived(new RealtimeEventDto());
        await staleStarted.Task;
        await realtime.Popover.Find("button[data-notification-action='open']")
            .ClickAsync(new MouseEventArgs());
        cut.Instance.Dispose();
        cut.Dispose();
        releaseStale.TrySetResult();
        await staleRefresh;
        await realtime.Handlers.NotificationReceived(new RealtimeEventDto());

        Assert.Equal(2, handler.CountCalls);
        Assert.Equal(1, handler.ListCalls);
        Assert.Equal(1, handler.MarkCalls);
    }

    private async Task<RealtimeHarness> ConfigureAsync(NotificationHttpHandler handler)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices(options =>
            options.PopoverOptions.CheckForPopoverProvider = false);
        var authorization = AddAuthorization();
        authorization.SetAuthorized("Test user");

        var factory = new StubHttpClientFactory(handler);
        Services.AddSingleton<IHttpClientFactory>(factory);
        Services.AddSingleton(new NotificationsClient(factory));

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
        var popover = Render<MudPopoverProvider>();
        return new RealtimeHarness(realtimeClient, handlers!, popover);
    }

    void IDisposable.Dispose()
    {
        Services.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.Dispose();
    }

    private static NotificationDto Notification(Guid id, bool isRead) => new()
    {
        Id = id,
        Title = "Order update",
        Body = "The order changed.",
        CreatedAtUtc = new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc),
        IsRead = isRead,
        LinkUrl = "/ui/orders/00000000-0000-0000-0000-000000000001"
    };

    private static HttpResponseMessage CountResponse(int count) => JsonResponse(
        HttpStatusCode.OK,
        new
        {
            success = true,
            data = new UnreadNotificationCountDto { Count = count }
        });

    private static HttpResponseMessage ListResponse(NotificationDto notification) => JsonResponse(
        HttpStatusCode.OK,
        new
        {
            success = true,
            data = new PagedResultDto<NotificationDto>
            {
                Items = [notification],
                Page = 1,
                PageSize = 20,
                TotalCount = 1
            }
        });

    private static HttpResponseMessage CommandResponse(bool success) => JsonResponse(
        success ? HttpStatusCode.OK : HttpStatusCode.Conflict,
        new { success, message = success ? "ok" : "failed" });

    private static HttpResponseMessage JsonResponse<T>(HttpStatusCode status, T value) =>
        new(status) { Content = JsonContent.Create(value) };

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task EnsureBellMenuOpenAsync(
        IRenderedComponent<MudPopoverProvider> popover,
        IRenderedComponent<NotificationBell> cut)
    {
        await cut.InvokeAsync(() => cut.Find("button[aria-label='Notifications']").Click());
        popover.WaitForElement("button[data-notification-action='open']");
    }

    private sealed class NotificationHttpHandler : HttpMessageHandler
    {
        public Func<int, CancellationToken, Task<HttpResponseMessage>> CountResponse { get; init; } =
            (_, _) => Task.FromResult(NotificationComponentConcurrencyTests.CountResponse(0));
        public Func<int, CancellationToken, Task<HttpResponseMessage>> ListResponse { get; init; } =
            (_, _) => Task.FromResult(NotificationComponentConcurrencyTests.ListResponse(
                Notification(Guid.NewGuid(), isRead: true)));
        public Func<int, CancellationToken, Task<HttpResponseMessage>> MarkResponse { get; init; } =
            (_, _) => Task.FromResult(CommandResponse(success: true));
        public Func<int, CancellationToken, Task<HttpResponseMessage>> MarkAllResponse { get; init; } =
            (_, _) => Task.FromResult(CommandResponse(success: true));

        public int CountCalls { get; private set; }
        public int ListCalls { get; private set; }
        public int MarkCalls { get; private set; }
        public int MarkAllCalls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Get && path == "/api/notifications/unread-count")
            {
                return CountResponse(++CountCalls, cancellationToken);
            }

            if (request.Method == HttpMethod.Get && path == "/api/notifications")
            {
                return ListResponse(++ListCalls, cancellationToken);
            }

            if (request.Method == HttpMethod.Post && path == "/api/notifications/read-all")
            {
                return MarkAllResponse(++MarkAllCalls, cancellationToken);
            }

            if (request.Method == HttpMethod.Post && path.EndsWith("/read", StringComparison.Ordinal))
            {
                return MarkResponse(++MarkCalls, cancellationToken);
            }

            throw new InvalidOperationException($"Unexpected endpoint {request.Method} {path}");
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri("https://localhost/") };
    }

    private sealed record RealtimeHarness(
        RealtimeClient Client,
        RealtimeConnectionHandlers Handlers,
        IRenderedComponent<MudPopoverProvider> Popover);

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
