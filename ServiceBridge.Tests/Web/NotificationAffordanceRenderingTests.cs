using Bunit;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Web.Components.Shared;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class NotificationAffordanceRenderingTests : BunitContext
{
    [Fact]
    public void ValidRoute_RendersAccessibleNavigationActionsWithoutNestedButtons()
    {
        var notification = Notification(isRead: false, "/ui/orders/42");
        var opened = 0;
        var bell = Render<NotificationBellItem>(parameters => parameters
            .Add(component => component.Notification, notification)
            .Add(component => component.OnOpen, _ => opened++));
        var list = Render<NotificationListActions>(parameters => parameters
            .Add(component => component.Notification, notification)
            .Add(component => component.OnOpen, _ => opened++));

        bell.Find("button[data-notification-action='open']").Click();
        list.Find("button[data-notification-action='open']").Click();

        Assert.Equal(2, opened);
        Assert.Equal(
            "Open notification: Order update",
            list.Find("button[data-notification-action='open']").GetAttribute("aria-label"));
        Assert.Equal(
            "Mark notification as read: Order update",
            list.Find("button[data-notification-action='mark-read']").GetAttribute("aria-label"));
        Assert.Empty(bell.FindAll("button button"));
        Assert.Empty(list.FindAll("button button"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/ui/%ZZ")]
    [InlineData("https://external.example")]
    public void MissingOrUnsafeRoute_UnreadNotificationRendersOnlyExplicitMarkRead(
        string? linkUrl)
    {
        var notification = Notification(isRead: false, linkUrl);
        var marked = 0;
        var bell = Render<NotificationBellItem>(parameters => parameters
            .Add(component => component.Notification, notification)
            .Add(component => component.OnMarkAsRead, _ => marked++));
        var list = Render<NotificationListActions>(parameters => parameters
            .Add(component => component.Notification, notification)
            .Add(component => component.OnMarkAsRead, _ => marked++));

        Assert.Empty(bell.FindAll("[data-notification-action='open']"));
        Assert.Empty(list.FindAll("[data-notification-action='open']"));
        bell.Find("button[data-notification-action='mark-read']").Click();
        list.Find("button[data-notification-action='mark-read']").Click();

        Assert.Equal(2, marked);
        Assert.Empty(bell.FindAll("button button"));
        Assert.Empty(list.FindAll("button button"));
    }

    [Fact]
    public void ReadNotificationWithoutRoute_IsNotInteractive()
    {
        var notification = Notification(isRead: true, linkUrl: null);
        var bell = Render<NotificationBellItem>(parameters => parameters
            .Add(component => component.Notification, notification));
        var list = Render<NotificationListActions>(parameters => parameters
            .Add(component => component.Notification, notification));

        Assert.Empty(bell.FindAll("button"));
        Assert.Empty(bell.FindAll("a"));
        Assert.Empty(list.FindAll("button"));
        Assert.Empty(list.FindAll("a"));
    }

    [Fact]
    public void Menu_PreservesMarkAllInteractionAndViewAllDestination()
    {
        var markAll = 0;
        var menu = Render<NotificationBellMenu>(parameters => parameters
            .Add(component => component.UnreadCount, 2)
            .Add(component => component.OnMarkAllAsRead, () => markAll++)
            .AddChildContent("<p>Notification list</p>"));

        menu.Find("button[data-notification-action='mark-all']").Click();

        Assert.Equal(1, markAll);
        Assert.Equal(
            "/ui/notifications",
            menu.Find("a[data-notification-action='view-all']").GetAttribute("href"));
    }

    private static NotificationDto Notification(bool isRead, string? linkUrl)
    {
        return new NotificationDto
        {
            Id = Guid.NewGuid(),
            Title = "Order update",
            Body = "The order changed.",
            CreatedAtUtc = new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc),
            IsRead = isRead,
            LinkUrl = linkUrl
        };
    }
}
