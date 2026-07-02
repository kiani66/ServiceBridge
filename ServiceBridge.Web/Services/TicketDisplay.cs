using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Web.Services;

public static class TicketDisplay
{
    public static string StatusLabel(string status)
    {
        return SplitPascalCase(status);
    }

    public static string PriorityLabel(string priority)
    {
        return SplitPascalCase(priority);
    }

    public static string StatusTone(string status)
    {
        return status switch
        {
            nameof(TicketStatus.Open) => "info",
            nameof(TicketStatus.WaitingForSupport) => "warning",
            nameof(TicketStatus.WaitingForCustomer) => "success",
            nameof(TicketStatus.Resolved) => "success",
            nameof(TicketStatus.Closed) => "neutral",
            _ => "neutral"
        };
    }

    public static string PriorityTone(string priority)
    {
        return priority switch
        {
            nameof(TicketPriority.Low) => "neutral",
            nameof(TicketPriority.Normal) => "info",
            nameof(TicketPriority.High) => "warning",
            nameof(TicketPriority.Urgent) => "danger",
            _ => "neutral"
        };
    }

    public static string FormatDateTime(DateTime value)
    {
        return value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }

    public static string FormatDateTime(DateTime? value)
    {
        return value.HasValue ? FormatDateTime(value.Value) : "-";
    }

    public static bool IsClosed(string status)
    {
        return status is nameof(TicketStatus.Closed) or nameof(TicketStatus.Resolved);
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character)
                ? $" {character}"
                : character.ToString()));
    }
}
