namespace ServiceBridge.Domain.Enums;

public enum MessageStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    Cancelled = 5
}
