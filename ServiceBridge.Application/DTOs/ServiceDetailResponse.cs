using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class ServiceDetailResponse : ServiceSummaryResponse
{
    public bool SupportsUseExistingAccount { get; set; } = true;
    public bool SupportsCreateNewAccount { get; set; }
    public IReadOnlyCollection<OrderFulfillmentMode> SupportedFulfillmentModes { get; set; } =
        Array.Empty<OrderFulfillmentMode>();
    public IReadOnlyCollection<ServiceVariantResponse> Plans { get; set; } =
        Array.Empty<ServiceVariantResponse>();
    public IReadOnlyCollection<ServiceRequiredFieldResponse> RequiredFields { get; set; } =
        Array.Empty<ServiceRequiredFieldResponse>();
}
