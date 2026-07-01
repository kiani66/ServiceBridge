namespace ServiceBridge.Application.DTOs;

public class AgentSummaryDto
{
    public Guid AgentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int ActiveOrderCount { get; set; }
    public int AssignedOrderCount { get; set; }
}
