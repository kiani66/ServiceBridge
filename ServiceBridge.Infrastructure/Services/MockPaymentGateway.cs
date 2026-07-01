using System.Text.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Payments;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Services;

public sealed class MockPaymentGateway : IMockPaymentGateway
{
    public PaymentGatewayCallback CreateCallback(Guid paymentId, bool succeed)
    {
        var processedAtUtc = DateTime.UtcNow;
        var gatewayReference = CreateGatewayReference(processedAtUtc, paymentId);
        var status = succeed ? PaymentStatus.Completed : PaymentStatus.Failed;
        var failureReason = succeed ? null : "Mock gateway declined the payment.";
        var request = new ConfirmPaymentRequest
        {
            PaymentId = paymentId,
            GatewayReference = gatewayReference,
            Status = status,
            FailureReason = failureReason
        };

        var payload = JsonSerializer.Serialize(
            new MockPaymentCallbackPayload(
                "MockGateway",
                paymentId,
                status.ToString(),
                gatewayReference,
                succeed,
                failureReason,
                processedAtUtc),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return new PaymentGatewayCallback(request, payload);
    }

    private static string CreateGatewayReference(DateTime processedAtUtc, Guid paymentId)
    {
        return $"MOCK-{processedAtUtc:yyyyMMddHHmmss}-{paymentId:N}"[..32];
    }

    private sealed record MockPaymentCallbackPayload(
        string Gateway,
        Guid PaymentId,
        string Status,
        string GatewayReference,
        bool Approved,
        string? FailureReason,
        DateTime ProcessedAtUtc);
}
