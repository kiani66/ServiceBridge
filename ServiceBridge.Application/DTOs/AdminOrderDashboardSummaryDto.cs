namespace ServiceBridge.Application.DTOs;

public class AdminOrderDashboardSummaryDto
{
    public int PendingPaymentOrderCount { get; set; }
    public int PaidOrderCount { get; set; }
    public int UnassignedOrderCount { get; set; }
}
