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
    [InlineData("   ")]
    [InlineData("https://external.example")]
    [InlineData("http://external.example")]
    [InlineData("//external.example")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,unsafe")]
    [InlineData("/ui/\\external.example")]
    [InlineData("/ui/orders/42\r\n")]
    [InlineData("/ui/%ZZ")]
    [InlineData("/ui/%")]
    [InlineData("/ui/%1")]
    [InlineData("/ui/orders/%5cevil")]
    [InlineData("/ui/orders/%5Cevil")]
    [InlineData("/ui/orders/42%0d%0aX")]
    [InlineData("/ui/orders/42%00")]
    [InlineData("/ui/orders/42%1f")]
    [InlineData("/ui/orders/42%7f")]
    [InlineData("/ui/../admin")]
    [InlineData("/ui/./orders")]
    [InlineData("/ui/%2e%2e/admin")]
    [InlineData("/ui/%2E%2E/admin")]
    [InlineData("/ui/%252e%252e/admin")]
    [InlineData("/ui/%25252e%25252e/admin")]
    [InlineData("/ui/%2525252e%2525252e/admin")]
    [InlineData("/ui/%252525252e%252525252e/admin")]
    [InlineData("/ui/%2e%2e/%2fadmin")]
    [InlineData("/ui/orders/%2Fadmin")]
    [InlineData("/ui/orders/%252Fadmin")]
    [InlineData("/ui/orders/%00")]
    [InlineData("/ui/orders/%EF%BC%8E%EF%BC%8E")]
    [InlineData("/ui/orders/%EF%BC%8Fadmin")]
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
    [InlineData("/ui/orders/42?tab=timeline&label=Order%20history")]
    [InlineData("/ui/orders/42#activity")]
    [InlineData("/ui/orders/42?tab=timeline#activity")]
    [InlineData("/ui/search?q=%E2%9C%93")]
    [InlineData("/ui/search?q=100%25")]
    [InlineData("/ui/orders/42?tab=timeline")]
    [InlineData("/ui/search/safe%20text")]
    [InlineData("/ui/search/100%25")]
    [InlineData("/ui/search/\u0633\u0641\u0627\u0631\u0634")]
    [InlineData("/ui/search?q=%ZZ#fragment%")]
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
