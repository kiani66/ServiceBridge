using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakePaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IReadOnlyDictionary<string, IPaymentGateway> _gateways;
    private readonly string _defaultProvider;

    public FakePaymentGatewayFactory(
        IEnumerable<IPaymentGateway> gateways,
        string defaultProvider = "Mock")
    {
        _gateways = gateways.ToDictionary(
            gateway => gateway.Provider,
            StringComparer.OrdinalIgnoreCase);
        _defaultProvider = defaultProvider;
    }

    public ServiceResult<IPaymentGateway> GetGateway(string? provider)
    {
        var selectedProvider = string.IsNullOrWhiteSpace(provider)
            ? _defaultProvider
            : provider.Trim();

        return _gateways.TryGetValue(selectedProvider, out var gateway)
            ? ServiceResult<IPaymentGateway>.Ok(gateway)
            : ServiceResult<IPaymentGateway>.Fail(
                AppMessages.PaymentGatewayUnsupported,
                AppConstants.ErrorCodes.ValidationFailed,
                400);
    }
}
