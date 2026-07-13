using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Web.Services;

public static class NotificationNavigation
{
    public static async Task<int> OpenAsync(
        NotificationDto notification,
        int unreadCount,
        Func<Task> closeAsync,
        Func<Guid, Task<NotificationsApiResult<bool>>> markAsReadAsync,
        Action<string> navigate)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(closeAsync);
        ArgumentNullException.ThrowIfNull(markAsReadAsync);
        ArgumentNullException.ThrowIfNull(navigate);

        try
        {
            await closeAsync();
        }
        catch
        {
            // A popover close failure must not prevent the notification action.
        }

        if (!notification.IsRead)
        {
            try
            {
                var result = await markAsReadAsync(notification.Id);
                if (result.Success)
                {
                    notification.IsRead = true;
                    unreadCount = Math.Max(0, unreadCount - 1);
                }
            }
            catch
            {
                // Navigation remains available when the read update fails unexpectedly.
            }
        }

        if (TryGetSafeInternalRoute(notification.LinkUrl, out var route))
        {
            navigate(route);
        }

        return unreadCount;
    }

    public static bool TryGetSafeInternalRoute(
        string? linkUrl,
        out string route)
    {
        route = string.Empty;
        if (string.IsNullOrWhiteSpace(linkUrl) ||
            !linkUrl.StartsWith("/ui/", StringComparison.Ordinal) ||
            linkUrl.StartsWith("//", StringComparison.Ordinal) ||
            linkUrl.Contains('\\') ||
            linkUrl.Any(char.IsControl) ||
            !HasValidPercentEncoding(linkUrl) ||
            DecodesToDangerousCharacters(linkUrl) ||
            !Uri.TryCreate(linkUrl, UriKind.Relative, out _))
        {
            return false;
        }

        route = linkUrl;
        return true;
    }

    private static bool HasValidPercentEncoding(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '%')
            {
                continue;
            }

            if (index + 2 >= value.Length ||
                !Uri.IsHexDigit(value[index + 1]) ||
                !Uri.IsHexDigit(value[index + 2]))
            {
                return false;
            }

            index += 2;
        }

        return true;
    }

    private static bool DecodesToDangerousCharacters(string value)
    {
        var decoded = Uri.UnescapeDataString(value);
        return decoded.Contains('\\') || decoded.Any(char.IsControl);
    }
}
