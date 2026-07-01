namespace ServiceBridge.Application.DTOs;

public class AgentOrderDashboardSummaryDto
{
    public int AssignedOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int CompletedToday { get; set; }
    public int FailedToday { get; set; }
}
