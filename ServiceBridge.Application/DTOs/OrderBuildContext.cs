namespace ServiceBridge.Application.DTOs;

public class OrderBuildContext
{
    public Guid UserId { get; set; }
    public CreateOrderRequest Request { get; set; } = null!;
    public OrderServiceDefinition Service { get; set; } = null!;
    public PricingCalculation Pricing { get; set; } = null!;
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
