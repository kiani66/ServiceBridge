using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class OrderServiceDefinition
{
    public Guid ServiceId { get; set; }
    public Guid CategoryId { get; set; }
    public bool HasActivePlans { get; set; }
    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }
    public int? PlanDurationDays { get; set; }
    public string? PlanRegionCode { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public bool SupportsUseExistingAccount { get; set; } = true;
    public bool SupportsCreateNewAccount { get; set; }
    public IReadOnlyCollection<OrderFieldDefinition> RequiredFields { get; set; } =
        Array.Empty<OrderFieldDefinition>();

    public bool SupportsFulfillmentMode(OrderFulfillmentMode mode)
    {
        return mode switch
        {
            OrderFulfillmentMode.UseExistingAccount => SupportsUseExistingAccount,
            OrderFulfillmentMode.CreateNewAccount => SupportsCreateNewAccount,
            _ => false
        };
    }
}
