using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class OrderServiceDefinition
{
    public Guid ServiceId { get; set; }
    public Guid CategoryId { get; set; }
    public bool HasActiveVariants { get; set; }
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int? VariantDurationDays { get; set; }
    public string? VariantRegionCode { get; set; }
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
