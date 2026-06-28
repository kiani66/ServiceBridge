namespace ServiceBridge.Domain.Enums;

public enum OrderStatus
{
    Created = 1,
    WaitingForPayment = 2,
    Paid = 3,
    Assigned = 4,
    Processing = 5,
    Completed = 7,
    Cancelled = 8,
    Failed = 11
}
