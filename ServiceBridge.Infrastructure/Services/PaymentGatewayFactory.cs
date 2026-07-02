using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Application.Settings;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public sealed class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IReadOnlyDictionary<string, IPaymentGateway> _gateways;
    private readonly PaymentGatewayOptions _options;
    private readonly ILogger<PaymentGatewayFactory> _logger;

    public PaymentGatewayFactory(
        IEnumerable<IPaymentGateway> gateways,
        IOptions<PaymentGatewayOptions> options,
        ILogger<PaymentGatewayFactory> logger)
    {
        _gateways = gateways.ToDictionary(
            gateway => gateway.Provider,
            StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
        _logger = logger;
    }

    public ServiceResult<IPaymentGateway> GetGateway(string? provider)
    {
        var selectedProvider = string.IsNullOrWhiteSpace(provider)
            ? _options.DefaultProvider
            : provider.Trim();

        if (_gateways.TryGetValue(selectedProvider, out var gateway))
        {
            return ServiceResult<IPaymentGateway>.Ok(gateway);
        }

        _logger.LogWarning(
            "Unsupported payment gateway provider requested: {Provider}",
            SafeProviderName(selectedProvider));

        return ServiceResult<IPaymentGateway>.Fail(
            AppMessages.PaymentGatewayUnsupported,
            AppConstants.ErrorCodes.ValidationFailed,
            400);
    }

    private static string SafeProviderName(string provider)
    {
        return provider.Length <= 50 ? provider : provider[..50];
    }
}
