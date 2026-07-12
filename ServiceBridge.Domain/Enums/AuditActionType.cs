namespace ServiceBridge.Domain.Enums;

public enum AuditActionType
{
    UserCreated = 1,
    UserActivated = 2,
    UserDeactivated = 3,
    AgentCreated = 4,
    OrderAssigned = 5,
    OrderReassigned = 6,
    OrderCancelledByAdmin = 7,
    CredentialViewed = 8,
    PaymentManuallyReviewed = 9,
    RefundRequested = 10,
    RefundConfirmed = 11,
    SettingsChanged = 12,
    TicketClosed = 13,
    InventoryItemCreated = 14,
    InventoryItemDisabled = 15
}
