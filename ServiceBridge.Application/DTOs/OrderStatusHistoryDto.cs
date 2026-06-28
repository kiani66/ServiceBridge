namespace ServiceBridge.Application.DTOs;

public class OrderStatusHistoryDto
{
    public string? PreviousStatus { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
