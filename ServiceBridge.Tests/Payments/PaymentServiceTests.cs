using Microsoft.Extensions.Logging.Abstractions;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Payments;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Payments;

public class PaymentServiceTests
{
    [Fact]
    public async Task InitiateAsync_WhenRequestIsValid_CreatesPendingSnapshot()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, OrderStatus.WaitingForPayment);
        var repository = new FakePaymentRepository { Order = order };
        var gateway = new FakePaymentGateway();
        var service = CreateService(repository, paymentGateway: gateway);
        var paymentId = Guid.NewGuid();

        var result = await service.InitiateAsync(userId, new InitiatePaymentRequest
        {
            OrderId = order.Id,
            PaymentId = paymentId
        });

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(paymentId, result.Data!.PaymentId);

        var payment = Assert.IsType<Payment>(repository.AddedPayment);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(order.FinalAmountIrr, payment.AmountIRR);
        Assert.Equal(order.ForeignPrice, payment.AmountUSD);
        Assert.Equal(order.ExchangeRateSnapshot, payment.ExchangeRateSnapshot);
        Assert.Equal(PaymentEventType.Initiated, Assert.Single(payment.Events).EventType);
        Assert.Equal(1, repository.SaveCount);
        Assert.Equal(1, gateway.InitiateCallCount);
        Assert.Equal(order.FinalAmountIrr, gateway.LastInitiateRequest!.AmountIRR);
        Assert.Equal("Mock", result.Data!.Provider);
        Assert.Equal("Mock-init", result.Data.GatewayReference);
    }

    [Fact]
    public async Task InitiateAsync_WhenSelectedGatewayIsDisabled_ReturnsControlledFailure()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, OrderStatus.WaitingForPayment);
        var repository = new FakePaymentRepository { Order = order };
        var gateway = new FakePaymentGateway
        {
            Provider = PaymentGatewayProviders.Real,
            InitiateResult = ServiceResult<PaymentGatewayInitiateResult>.Fail(
                AppMessages.PaymentGatewayDisabled,
                AppConstants.ErrorCodes.PaymentGatewayUnavailable,
                503)
        };
        var service = CreateService(repository, paymentGateway: gateway);

        var result = await service.InitiateAsync(
            userId,
            new InitiatePaymentRequest
            {
                OrderId = order.Id,
                PaymentId = Guid.NewGuid(),
                GatewayProvider = PaymentGatewayProviders.Real
            });

        Assert.False(result.Success);
        Assert.Equal(503, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.PaymentGatewayUnavailable, result.ErrorCode);
        Assert.Null(repository.AddedPayment);
        Assert.Equal(0, repository.SaveCount);
        Assert.Equal(1, gateway.InitiateCallCount);
    }

    [Fact]
    public async Task InitiateAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService(new FakePaymentRepository());

        var result = await service.InitiateAsync(Guid.NewGuid(), ValidRequest(Guid.NewGuid()));

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task InitiateAsync_WhenOrderBelongsToAnotherUser_ReturnsForbidden()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.WaitingForPayment);
        var service = CreateService(new FakePaymentRepository { Order = order });

        var result = await service.InitiateAsync(Guid.NewGuid(), ValidRequest(order.Id));

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Forbidden, result.ErrorCode);
    }

    [Fact]
    public async Task InitiateAsync_WhenOrderIsNotPending_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, OrderStatus.Paid);
        var service = CreateService(new FakePaymentRepository { Order = order });

        var result = await service.InitiateAsync(userId, ValidRequest(order.Id));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
    }

    [Fact]
    public async Task InitiateAsync_WhenPendingPaymentExists_ReturnsExistingPayment()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, OrderStatus.WaitingForPayment);
        var existingPayment = CreatePayment(order);
        var service = CreateService(new FakePaymentRepository
        {
            Order = order,
            ExistingPendingPayment = existingPayment
        });

        var result = await service.InitiateAsync(userId, ValidRequest(order.Id));

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(existingPayment.Id, result.Data!.PaymentId);
    }

    [Fact]
    public async Task InitiateAsync_WhenPendingPaymentExists_DoesNotCreateDuplicatePayment()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, OrderStatus.WaitingForPayment);
        var existingPayment = CreatePayment(order);
        var repository = new FakePaymentRepository
        {
            Order = order,
            ExistingPendingPayment = existingPayment
        };
        var service = CreateService(repository);

        var result = await service.InitiateAsync(userId, ValidRequest(order.Id));

        Assert.True(result.Success);
        Assert.Equal(existingPayment.Id, result.Data!.PaymentId);
        Assert.Null(repository.AddedPayment);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ProcessMockAsync_WhenReusedPendingPaymentSucceeds_ConfirmsAndMarksOrderPaid()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, OrderStatus.WaitingForPayment);
        var existingPayment = CreatePayment(order);
        var repository = new FakePaymentRepository
        {
            Order = order,
            Payment = existingPayment,
            ExistingPendingPayment = existingPayment
        };
        var service = CreateService(repository);

        var initiated = await service.InitiateAsync(userId, ValidRequest(order.Id));
        var processed = await service.ProcessMockAsync(
            userId,
            new MockPaymentRequest
            {
                PaymentId = initiated.Data!.PaymentId,
                Succeed = true
            });

        Assert.True(initiated.Success);
        Assert.True(processed.Success);
        Assert.Equal(existingPayment.Id, processed.Data!.PaymentId);
        Assert.Equal(PaymentStatus.Completed, existingPayment.Status);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task ConfirmAsync_WhenPaymentIsPending_ConfirmsAndMarksOrderPaid()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.WaitingForPayment);
        var payment = CreatePayment(order);
        var repository = new FakePaymentRepository { Payment = payment };
        var service = CreateService(repository);

        var result = await service.ConfirmAsync(
            new ConfirmPaymentRequest
            {
                PaymentId = payment.Id,
                GatewayReference = "gateway-123"
            },
            """{"paymentId":"test"}""");

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("gateway-123", payment.GatewayReference);
        Assert.NotNull(payment.ConfirmedAt);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.NotNull(order.PaidAt);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task ConfirmAsync_WhenPayloadContainsSecrets_RedactsStoredPayload()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.WaitingForPayment);
        var payment = CreatePayment(order);
        var repository = new FakePaymentRepository { Payment = payment };
        var service = CreateService(repository);

        var result = await service.ConfirmAsync(
            new ConfirmPaymentRequest
            {
                PaymentId = payment.Id,
                GatewayReference = "gateway-123"
            },
            """{"paymentId":"test","apiKey":"secret-value","nested":{"token":"token-value"}}""");

        Assert.True(result.Success);
        var paymentEvent = Assert.Single(payment.Events);
        Assert.Contains("[REDACTED]", paymentEvent.Payload);
        Assert.DoesNotContain("secret-value", paymentEvent.Payload);
        Assert.DoesNotContain("token-value", paymentEvent.Payload);
    }

    [Fact]
    public async Task ConfirmAsync_WhenPaymentDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService(new FakePaymentRepository());

        var result = await service.ConfirmAsync(
            new ConfirmPaymentRequest { PaymentId = Guid.NewGuid() },
            "{}");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task ConfirmAsync_WhenCompletedPaymentReceivesDuplicateCompletedCallback_ReturnsOk()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.Paid);
        var payment = CreatePayment(order);
        payment.Complete("existing", "{}", DateTime.UtcNow);
        var repository = new FakePaymentRepository { Payment = payment };
        var service = CreateService(repository);

        var result = await service.ConfirmAsync(
            new ConfirmPaymentRequest { PaymentId = payment.Id },
            "{}");

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ConfirmAsync_WhenCompletedPaymentReceivesFailedCallback_ReturnsConflict()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.Paid);
        var payment = CreatePayment(order);
        payment.Complete("existing", "{}", DateTime.UtcNow);
        var service = CreateService(new FakePaymentRepository { Payment = payment });

        var result = await service.ConfirmAsync(
            new ConfirmPaymentRequest
            {
                PaymentId = payment.Id,
                Status = PaymentStatus.Failed,
                FailureReason = "Gateway declined."
            },
            "{}");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
    }

    [Fact]
    public async Task ConfirmAsync_WhenGatewayFails_RecordsFailureAndKeepsOrderPending()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.WaitingForPayment);
        var payment = CreatePayment(order);
        var repository = new FakePaymentRepository { Payment = payment };
        var service = CreateService(repository);

        var result = await service.ConfirmAsync(
            new ConfirmPaymentRequest
            {
                PaymentId = payment.Id,
                Status = PaymentStatus.Failed,
                FailureReason = "Gateway declined the transaction."
            },
            "{}");

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(OrderStatus.WaitingForPayment, order.Status);
        Assert.Equal(
            PaymentEventType.Failed,
            Assert.Single(payment.Events).EventType);
    }

    [Fact]
    public async Task VerifyCallbackAsync_WhenGatewayVerificationFails_RecordsFailureSafely()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.WaitingForPayment);
        var payment = CreatePayment(order);
        var repository = new FakePaymentRepository { Payment = payment };
        var rawPayload =
            $$"""{"paymentId":"{{payment.Id}}","apiKey":"secret-value","status":"Rejected"}""";
        var gateway = new FakePaymentGateway
        {
            VerifyResult = new PaymentGatewayVerifyResult
            {
                Provider = PaymentGatewayProviders.Mock,
                IsVerified = false,
                PaymentId = payment.Id,
                Status = PaymentStatus.Failed,
                FailureReason = "Gateway signature verification failed.",
                SanitizedPayload = PaymentGatewayPayloadSanitizer.Sanitize(rawPayload)
            }
        };
        var service = CreateService(repository, paymentGateway: gateway);

        var result = await service.VerifyCallbackAsync(
            new PaymentGatewayVerifyRequest(
                PaymentGatewayProviders.Mock,
                rawPayload));

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(OrderStatus.WaitingForPayment, order.Status);
        var paymentEvent = Assert.Single(payment.Events);
        Assert.Equal(PaymentEventType.Failed, paymentEvent.EventType);
        Assert.Contains("[REDACTED]", paymentEvent.Payload);
        Assert.DoesNotContain("secret-value", paymentEvent.Payload);
        Assert.Equal(1, gateway.VerifyCallCount);
    }

    [Fact]
    public async Task ConfirmAsync_WhenCompletedPaymentIsRefunded_RecordsRefund()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.Paid);
        var payment = CreatePayment(order);
        payment.Complete("gateway-1", "{}", DateTime.UtcNow);
        var repository = new FakePaymentRepository { Payment = payment };
        var service = CreateService(repository);

        var result = await service.ConfirmAsync(
            new ConfirmPaymentRequest
            {
                PaymentId = payment.Id,
                Status = PaymentStatus.Refunded,
                GatewayReference = "refund-1"
            },
            "{}");

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.NotNull(payment.RefundedAt);
        Assert.Equal(2, payment.Events.Count);
    }

    [Fact]
    public async Task ProcessMockAsync_WhenGatewaySucceeds_ConfirmsAndMarksOrderPaid()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, OrderStatus.WaitingForPayment);
        var payment = CreatePayment(order);
        var repository = new FakePaymentRepository { Payment = payment };
        var gateway = new FakeMockPaymentGateway();
        var service = CreateService(repository, gateway);

        var result = await service.ProcessMockAsync(
            userId,
            new MockPaymentRequest
            {
                PaymentId = payment.Id,
                Succeed = true
            });

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Completed, result.Data!.Status);
        Assert.Equal(payment.Id, result.Data.PaymentId);
        Assert.Equal(order.Id, result.Data.OrderId);
        Assert.Equal("mock-success", result.Data.GatewayReference);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(1, repository.SaveCount);
        Assert.Equal(1, gateway.CallCount);
        Assert.True(gateway.LastOutcome);
    }

    [Fact]
    public async Task ProcessMockAsync_WhenGatewayFails_RecordsFailureAndKeepsOrderPending()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, OrderStatus.WaitingForPayment);
        var payment = CreatePayment(order);
        var repository = new FakePaymentRepository { Payment = payment };
        var gateway = new FakeMockPaymentGateway();
        var service = CreateService(repository, gateway);

        var result = await service.ProcessMockAsync(
            userId,
            new MockPaymentRequest
            {
                PaymentId = payment.Id,
                Succeed = false
            });

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Failed, result.Data!.Status);
        Assert.Equal("mock-failed", result.Data.GatewayReference);
        Assert.Equal(OrderStatus.WaitingForPayment, order.Status);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(1, repository.SaveCount);
        Assert.False(gateway.LastOutcome);
    }

    [Fact]
    public async Task ProcessMockAsync_WhenPaymentBelongsToAnotherUser_ReturnsForbidden()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.WaitingForPayment);
        var payment = CreatePayment(order);
        var gateway = new FakeMockPaymentGateway();
        var service = CreateService(
            new FakePaymentRepository { Payment = payment },
            gateway);

        var result = await service.ProcessMockAsync(
            Guid.NewGuid(),
            new MockPaymentRequest
            {
                PaymentId = payment.Id,
                Succeed = true
            });

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Forbidden, result.ErrorCode);
        Assert.Equal(0, gateway.CallCount);
        Assert.Equal(OrderStatus.WaitingForPayment, order.Status);
    }

    private static InitiatePaymentRequest ValidRequest(Guid orderId)
    {
        return new InitiatePaymentRequest
        {
            OrderId = orderId,
            PaymentId = Guid.NewGuid()
        };
    }

    private static Order CreateOrder(Guid userId, OrderStatus status)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = status,
            ForeignPrice = 20m,
            ExchangeRateSnapshot = 150_000m,
            MarginPercentSnapshot = 10m,
            BaseAmountIrr = 3_000_000m,
            ProfitAmountIrr = 300_000m,
            FinalAmountIrr = 3_300_000m
        };
    }

    private static Payment CreatePayment(Order order)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            AmountIRR = order.FinalAmountIrr,
            AmountUSD = order.ForeignPrice,
            ExchangeRateSnapshot = order.ExchangeRateSnapshot
        };
    }

    private static PaymentService CreateService(
        FakePaymentRepository repository,
        FakeMockPaymentGateway? gateway = null,
        FakePaymentGateway? paymentGateway = null)
    {
        paymentGateway ??= new FakePaymentGateway();

        return new PaymentService(
            repository,
            gateway ?? new FakeMockPaymentGateway(),
            new FakePaymentGatewayFactory([paymentGateway]),
            new FakeNotificationService(),
            NullLogger<PaymentService>.Instance);
    }
}
