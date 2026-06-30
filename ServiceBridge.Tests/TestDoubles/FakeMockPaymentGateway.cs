using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Payments;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeMockPaymentGateway : IMockPaymentGateway
{
    public int CallCount { get; private set; }
    public bool? LastOutcome { get; private set; }

    public PaymentGatewayCallback CreateCallback(Guid paymentId, bool succeed)
    {
        CallCount++;
        LastOutcome = succeed;

        var status = succeed ? PaymentStatus.Completed : PaymentStatus.Failed;
        var request = new ConfirmPaymentRequest
        {
            PaymentId = paymentId,
            GatewayReference = succeed ? "mock-success" : "mock-failed",
            Status = status,
            FailureReason = succeed ? null : "Mock gateway declined."
        };

        return new PaymentGatewayCallback(
            request,
            succeed
                ? """{"gateway":"MockGateway","status":"Completed"}"""
                : """{"gateway":"MockGateway","status":"Failed"}""");
    }
}
