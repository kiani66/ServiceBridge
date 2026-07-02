namespace ServiceBridge.Application.Payments;

public sealed record PaymentGatewayInitiateRequest(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    decimal AmountIRR,
    decimal AmountUSD,
    decimal ExchangeRateSnapshot);
