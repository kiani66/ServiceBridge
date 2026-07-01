using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Application.Payments;

public sealed record PaymentGatewayCallback(
    ConfirmPaymentRequest Request,
    string RawPayload);
