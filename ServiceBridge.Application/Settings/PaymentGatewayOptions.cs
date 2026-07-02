using ServiceBridge.Application.Payments;

namespace ServiceBridge.Application.Settings;

public sealed class PaymentGatewayOptions
{
    public const string SectionName = "PaymentGateway";

    public string DefaultProvider { get; set; } = PaymentGatewayProviders.Mock;
    public RealPaymentGatewayOptions Real { get; set; } = new();
}

public sealed class RealPaymentGatewayOptions
{
    public bool Enabled { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string CallbackBaseUrl { get; set; } = string.Empty;
}
