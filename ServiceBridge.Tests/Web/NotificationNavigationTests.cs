using ServiceBridge.Application.DTOs;
using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class NotificationNavigationTests
{
    [Fact]
    public async Task OpenAsync_UnreadNotification_ClosesMarksUpdatesBadgeAndNavigatesInOrder()
    {
        var notification = Notification(isRead: false, "/ui/orders/42");
        var events = new List<string>();

        var unreadCount = await NotificationNavigation.OpenAsync(
            notification,
            3,
            () =>
            {
                events.Add("close");
                return Task.CompletedTask;
            },
            id =>
            {
                events.Add($"mark:{id}");
                return Task.FromResult(NotificationsApiResult<bool>.Succeeded(true));
            },
            route => events.Add($"navigate:{route}"));

        Assert.Equal(
            ["close", $"mark:{notification.Id}", "navigate:/ui/orders/42"],
            events);
        Assert.True(notification.IsRead);
        Assert.Equal(2, unreadCount);
    }

    [Fact]
    public async Task OpenAsync_ReadNotification_DoesNotMarkAgain()
    {
        var notification = Notification(isRead: true, "/ui/tickets/42");
        var markAttempts = 0;
        string? navigatedTo = null;

        var unreadCount = await NotificationNavigation.OpenAsync(
            notification,
            4,
            () => Task.CompletedTask,
            id =>
            {
                markAttempts++;
                return Task.FromResult(NotificationsApiResult<bool>.Succeeded(true));
            },
            route => navigatedTo = route);

        Assert.Equal(0, markAttempts);
        Assert.Equal(4, unreadCount);
        Assert.Equal("/ui/tickets/42", navigatedTo);
    }

    [Fact]
    public async Task OpenAsync_MarkFailure_DoesNotBlockNavigationOrChangeUnreadState()
    {
        var notification = Notification(isRead: false, "/ui/agent/orders/42");
        string? navigatedTo = null;

        var unreadCount = await NotificationNavigation.OpenAsync(
            notification,
            1,
            () => Task.CompletedTask,
            id => Task.FromResult(NotificationsApiResult<bool>.Failed("failed")),
            route => navigatedTo = route);

        Assert.False(notification.IsRead);
        Assert.Equal(1, unreadCount);
        Assert.Equal("/ui/agent/orders/42", navigatedTo);
    }

    [Fact]
    public async Task OpenAsync_UnexpectedMarkException_DoesNotBlockNavigation()
    {
        var notification = Notification(isRead: false, "/ui/admin/orders/42");
        string? navigatedTo = null;

        await NotificationNavigation.OpenAsync(
            notification,
            1,
            () => Task.CompletedTask,
            id => throw new InvalidOperationException("simulated failure"),
            route => navigatedTo = route);

        Assert.Equal("/ui/admin/orders/42", navigatedTo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://external.example")]
    [InlineData("http://external.example")]
    [InlineData("//external.example")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/ui/\\external.example")]
    [InlineData("/ui/orders/42\r\n")]
    public async Task OpenAsync_MissingMalformedOrExternalLink_MarksSafelyWithoutNavigation(
        string? linkUrl)
    {
        var notification = Notification(isRead: false, linkUrl);
        var navigated = false;

        var unreadCount = await NotificationNavigation.OpenAsync(
            notification,
            1,
            () => Task.CompletedTask,
            id => Task.FromResult(NotificationsApiResult<bool>.Succeeded(true)),
            route => navigated = true);

        Assert.True(notification.IsRead);
        Assert.Equal(0, unreadCount);
        Assert.False(navigated);
    }

    [Theory]
    [InlineData("/ui/orders/42")]
    [InlineData("/ui/agent/orders/42")]
    [InlineData("/ui/admin/orders/42")]
    [InlineData("/ui/tickets/42")]
    [InlineData("/ui/admin/tickets/42?tab=messages")]
    public void TryGetSafeInternalRoute_AcceptsOnlyUiRelativeRoutes(string linkUrl)
    {
        Assert.True(NotificationNavigation.TryGetSafeInternalRoute(linkUrl, out var route));
        Assert.Equal(linkUrl, route);
    }

    private static NotificationDto Notification(bool isRead, string? linkUrl)
    {
        return new NotificationDto
        {
            Id = Guid.NewGuid(),
            IsRead = isRead,
            LinkUrl = linkUrl
        };
    }
}
