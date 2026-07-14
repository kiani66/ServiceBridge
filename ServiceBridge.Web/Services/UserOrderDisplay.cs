using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Web.Components.Shared;

namespace ServiceBridge.Web.Services;

public static class UserOrderDisplay
{
    public static string VariantLabel(
        string? planName,
        int? planDurationDays,
        string? planRegionCode)
    {
        if (string.IsNullOrWhiteSpace(planName))
        {
            return "-";
        }

        var parts = new List<string> { planName };
        if (planDurationDays.HasValue)
        {
            parts.Add($"{planDurationDays.Value} days");
        }

        if (!string.IsNullOrWhiteSpace(planRegionCode))
        {
            parts.Add(planRegionCode);
        }

        return string.Join(" / ", parts);
    }

    public static string OrderStatusLabel(string status)
    {
        return status switch
        {
            nameof(OrderStatus.WaitingForPayment) => "Waiting for payment",
            _ => SplitPascalCase(status)
        };
    }

    public static string PaymentStatusLabel(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "Not started";
        }

        return SplitPascalCase(status);
    }

    public static string RefundStatusLabel(string status)
    {
        return SplitPascalCase(status);
    }

    public static string OrderTone(string status)
    {
        return status switch
        {
            nameof(OrderStatus.Paid) or nameof(OrderStatus.Completed) => "success",
            nameof(OrderStatus.Cancelled) or nameof(OrderStatus.Failed) => "danger",
            nameof(OrderStatus.Created) or nameof(OrderStatus.WaitingForPayment) => "warning",
            nameof(OrderStatus.Assigned) or nameof(OrderStatus.Processing) => "info",
            _ => "neutral"
        };
    }

    public static string PaymentTone(string? status)
    {
        return status switch
        {
            nameof(PaymentStatus.Completed) => "success",
            nameof(PaymentStatus.Failed) or nameof(PaymentStatus.Refunded) => "danger",
            nameof(PaymentStatus.Pending) => "warning",
            _ => "neutral"
        };
    }

    public static string RefundTone(string status)
    {
        return status switch
        {
            nameof(RefundStatus.Approved) or nameof(RefundStatus.Completed) => "success",
            nameof(RefundStatus.Rejected) or nameof(RefundStatus.Cancelled) => "danger",
            nameof(RefundStatus.Requested) => "warning",
            _ => "neutral"
        };
    }

    public static bool CanRequestRefund(string status)
    {
        return status is nameof(OrderStatus.Paid) or
            nameof(OrderStatus.Assigned) or
            nameof(OrderStatus.Processing) or
            nameof(OrderStatus.Failed);
    }

    public static IReadOnlyList<TimelineItem> TimelineItems(
        IEnumerable<OrderStatusHistoryDto> history)
    {
        return history
            .Select(item => new TimelineItem(
                OrderStatusLabel(item.Status),
                item.PreviousStatus is null
                    ? "Order created."
                    : $"{OrderStatusLabel(item.PreviousStatus)} changed to {OrderStatusLabel(item.Status)}.",
                FormatDateTime(item.CreatedAt),
                OrderTone(item.Status)))
            .ToArray();
    }

    public static PaymentHistoryDto? LatestPayment(UserOrderDetailDto order)
    {
        return order.Payments
            .OrderByDescending(payment => payment.CreatedAt)
            .ThenByDescending(payment => payment.PaymentId)
            .FirstOrDefault();
    }

    public static string FormatDateTime(DateTime value)
    {
        return $"{value:yyyy-MM-dd HH:mm} UTC";
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
