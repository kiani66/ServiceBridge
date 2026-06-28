namespace ServiceBridge.Application.DTOs;

public class AgentOrderDetailDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FulfillmentMode { get; set; } = string.Empty;
    public string? PlanName { get; set; }
    public int? PlanDurationDays { get; set; }
    public string? PlanRegionCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public IReadOnlyCollection<AgentOrderFieldDto> Fields { get; set; } =
        Array.Empty<AgentOrderFieldDto>();
    public IReadOnlyCollection<AgentOrderFieldDto> DeliveryFields { get; set; } =
        Array.Empty<AgentOrderFieldDto>();
    public IReadOnlyCollection<AgentOrderNoteDto> Notes { get; set; } =
        Array.Empty<AgentOrderNoteDto>();
    public IReadOnlyCollection<CustomerActionRequestDto> CustomerActions { get; set; } =
        Array.Empty<CustomerActionRequestDto>();
    public IReadOnlyCollection<OrderStatusHistoryDto> StatusHistory { get; set; } =
        Array.Empty<OrderStatusHistoryDto>();
}
