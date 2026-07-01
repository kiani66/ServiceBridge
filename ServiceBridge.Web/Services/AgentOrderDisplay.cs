using ServiceBridge.Application.DTOs;
using ServiceBridge.Web.Components.Shared;

namespace ServiceBridge.Web.Services;

public static class AgentOrderDisplay
{
    public static string VariantLabel(
        string? planName,
        int? planDurationDays,
        string? planRegionCode)
    {
        return UserOrderDisplay.VariantLabel(
            planName,
            planDurationDays,
            planRegionCode);
    }

    public static string OrderStatusLabel(string status)
    {
        return UserOrderDisplay.OrderStatusLabel(status);
    }

    public static string OrderTone(string status)
    {
        return UserOrderDisplay.OrderTone(status);
    }

    public static string PaymentStatusLabel(string? status)
    {
        return UserOrderDisplay.PaymentStatusLabel(status);
    }

    public static string PaymentTone(string? status)
    {
        return UserOrderDisplay.PaymentTone(status);
    }

    public static IReadOnlyList<TimelineItem> TimelineItems(
        IEnumerable<OrderStatusHistoryDto> history)
    {
        return UserOrderDisplay.TimelineItems(history);
    }

    public static PaymentHistoryDto? LatestPayment(AgentOrderDetailDto order)
    {
        return order.Payments
            .OrderByDescending(payment => payment.CreatedAt)
            .ThenByDescending(payment => payment.PaymentId)
            .FirstOrDefault();
    }

    public static string FormatDateTime(DateTime value)
    {
        return UserOrderDisplay.FormatDateTime(value);
    }
}
