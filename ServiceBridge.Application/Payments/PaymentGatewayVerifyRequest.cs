namespace ServiceBridge.Application.Payments;

public sealed record PaymentGatewayVerifyRequest(
    string? Provider,
    string RawPayload);
