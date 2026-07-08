namespace ServiceBridge.Application.DTOs;

public static class RealtimeEventTypes
{
    public const string NotificationReceived = nameof(NotificationReceived);
    public const string OrderUpdated = nameof(OrderUpdated);
    public const string OrderAssigned = nameof(OrderAssigned);
    public const string CustomerActionChanged = nameof(CustomerActionChanged);
    public const string TicketUpdated = nameof(TicketUpdated);
}
