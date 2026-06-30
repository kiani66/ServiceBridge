using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class OrderServiceDefinition
{
    public Guid ServiceId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid ServiceVariantId { get; set; }
    public Guid? VariantId
    {
        get => ServiceVariantId == Guid.Empty ? null : ServiceVariantId;
        set => ServiceVariantId = value ?? Guid.Empty;
    }

    public bool IsVariantActive { get; set; } = true;
    public bool IsServiceActive { get; set; } = true;
    public bool IsCategoryActive { get; set; } = true;
    public bool IsProviderActive { get; set; } = true;
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
