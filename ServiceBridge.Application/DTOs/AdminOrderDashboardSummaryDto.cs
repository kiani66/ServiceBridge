namespace ServiceBridge.Application.DTOs;

public class AdminOrderDashboardSummaryDto
{
    public int TotalOrderCount { get; set; }
    public int PendingPaymentOrderCount { get; set; }
    public int PaidOrderCount { get; set; }
    public int ProcessingOrderCount { get; set; }
    public int CompletedOrderCount { get; set; }
    public int FailedOrderCount { get; set; }
    public int UnassignedOrderCount { get; set; }
}
