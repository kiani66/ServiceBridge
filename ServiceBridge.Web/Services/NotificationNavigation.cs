using ServiceBridge.Application.DTOs;
using System.Text;

namespace ServiceBridge.Web.Services;

public static class NotificationNavigation
{
    // Triple-encoded input is expected in the wild; one additional layer bounds work
    // while still rejecting values that remain transformable after the safety limit.
    internal const int MaxPathSegmentDecodeDepth = 4;

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

        var preparation = await PrepareOpenAsync(
            notification,
            closeAsync,
            markAsReadAsync,
            static () => true);
        if (preparation.MarkedAsRead)
        {
            notification.IsRead = true;
            unreadCount = Math.Max(0, unreadCount - 1);
        }

        if (preparation.Route is not null)
        {
            navigate(preparation.Route);
        }

        return unreadCount;
    }

    internal static async Task<NotificationOpenPreparation> PrepareOpenAsync(
        NotificationDto notification,
        Func<Task> closeAsync,
        Func<Guid, Task<NotificationsApiResult<bool>>> markAsReadAsync,
        Func<bool> canContinue)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(closeAsync);
        ArgumentNullException.ThrowIfNull(markAsReadAsync);
        ArgumentNullException.ThrowIfNull(canContinue);

        try
        {
            await closeAsync();
        }
        catch
        {
            // A popover close failure must not prevent the notification action.
        }

        var route = TryGetSafeInternalRoute(notification.LinkUrl, out var safeRoute)
            ? safeRoute
            : null;
        var markedAsRead = false;

        if (!notification.IsRead)
        {
            try
            {
                var result = await markAsReadAsync(notification.Id);
                markedAsRead = result.Success;
            }
            catch
            {
                // Navigation remains available when the read update fails unexpectedly.
            }
        }

        if (!canContinue())
        {
            return default;
        }

        return new NotificationOpenPreparation(markedAsRead, route);
    }

    public static bool TryGetSafeInternalRoute(
        string? linkUrl,
        out string route)
    {
        route = string.Empty;
        if (string.IsNullOrWhiteSpace(linkUrl) ||
            linkUrl.Any(char.IsControl) ||
            !Uri.TryCreate(linkUrl, UriKind.Relative, out _))
        {
            return false;
        }

        var pathEnd = linkUrl.IndexOfAny(['?', '#']);
        var path = pathEnd >= 0 ? linkUrl[..pathEnd] : linkUrl;
        if (!path.StartsWith("/ui/", StringComparison.Ordinal) ||
            path.StartsWith("//", StringComparison.Ordinal) ||
            path.Contains('\\'))
        {
            return false;
        }

        try
        {
            foreach (var segment in path.Split('/'))
            {
                if (!IsSafePathSegment(segment))
                {
                    return false;
                }
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        route = linkUrl;
        return true;
    }

    private static bool IsSafePathSegment(string segment)
    {
        if (!HasValidPercentEncoding(segment))
        {
            return false;
        }

        var current = segment;
        for (var depth = 0; depth < MaxPathSegmentDecodeDepth; depth++)
        {
            if (IsDangerousDecodedSegment(current))
            {
                return false;
            }

            var decoded = Uri.UnescapeDataString(current);
            if (decoded == current)
            {
                return true;
            }

            if (IsDangerousDecodedSegment(decoded))
            {
                return false;
            }

            current = decoded;
            if (!ContainsEncodedOctet(current))
            {
                return true;
            }

            if (!HasValidPercentEncoding(current))
            {
                return false;
            }
        }

        return !ContainsEncodedOctet(current);
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

    private static bool ContainsEncodedOctet(string value)
    {
        for (var index = 0; index + 2 < value.Length; index++)
        {
            if (value[index] == '%' &&
                Uri.IsHexDigit(value[index + 1]) &&
                Uri.IsHexDigit(value[index + 2]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDangerousDecodedSegment(string value)
    {
        return IsDangerousSegmentValue(value) ||
               IsDangerousSegmentValue(value.Normalize(NormalizationForm.FormKC));
    }

    private static bool IsDangerousSegmentValue(string value)
    {
        return value is "." or ".." ||
               value.Contains('/') ||
               value.Contains('\\') ||
               value.Any(char.IsControl);
    }
}

internal readonly record struct NotificationOpenPreparation(
    bool MarkedAsRead,
    string? Route);
