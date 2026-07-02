namespace ServiceBridge.Domain.Enums;

public enum NotificationType
{
    OrderCreated = 1,
    PaymentCompleted = 2,
    OrderAssignedToAgent = 3,
    AgentStartedProcessing = 4,
    OrderCompleted = 5,
    OrderFailed = 6,
    CustomerActionRequested = 7,
    TicketCreated = 8,
    TicketReplied = 9,
    TicketClosed = 10
}
