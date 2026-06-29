namespace ServiceBridge.Application.DTOs;

public class AgentOrderListItemDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public Guid? ServiceVariantId { get; set; }
    public string? PlanName { get; set; }
    public int? PlanDurationDays { get; set; }
    public string? PlanRegionCode { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public bool HasPendingCustomerAction { get; set; }
}
