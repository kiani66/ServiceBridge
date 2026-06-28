using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
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
        var service = new PaymentService(repository);
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
    }

    [Fact]
    public async Task InitiateAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var service = new PaymentService(new FakePaymentRepository());

        var result = await service.InitiateAsync(Guid.NewGuid(), ValidRequest(Guid.NewGuid()));

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task InitiateAsync_WhenOrderBelongsToAnotherUser_ReturnsForbidden()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.WaitingForPayment);
        var service = new PaymentService(new FakePaymentRepository { Order = order });

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
        var service = new PaymentService(new FakePaymentRepository { Order = order });

        var result = await service.InitiateAsync(userId, ValidRequest(order.Id));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
    }

    [Fact]
    public async Task InitiateAsync_WhenPendingPaymentExists_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, OrderStatus.WaitingForPayment);
        var service = new PaymentService(new FakePaymentRepository
        {
            Order = order,
            HasPendingPayment = true
        });

        var result = await service.InitiateAsync(userId, ValidRequest(order.Id));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
    }

    [Fact]
    public async Task ConfirmAsync_WhenPaymentIsPending_ConfirmsAndMarksOrderPaid()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.WaitingForPayment);
        var payment = CreatePayment(order);
        var repository = new FakePaymentRepository { Payment = payment };
        var service = new PaymentService(repository);

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
    public async Task ConfirmAsync_WhenPaymentDoesNotExist_ReturnsNotFound()
    {
        var service = new PaymentService(new FakePaymentRepository());

        var result = await service.ConfirmAsync(
            new ConfirmPaymentRequest { PaymentId = Guid.NewGuid() },
            "{}");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task ConfirmAsync_WhenPaymentWasConfirmed_ReturnsConflict()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.WaitingForPayment);
        var payment = CreatePayment(order);
        payment.Complete("existing", "{}", DateTime.UtcNow);
        var service = new PaymentService(new FakePaymentRepository { Payment = payment });

        var result = await service.ConfirmAsync(
            new ConfirmPaymentRequest { PaymentId = payment.Id },
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
        var service = new PaymentService(repository);

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
    public async Task ConfirmAsync_WhenCompletedPaymentIsRefunded_RecordsRefund()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.Paid);
        var payment = CreatePayment(order);
        payment.Complete("gateway-1", "{}", DateTime.UtcNow);
        var repository = new FakePaymentRepository { Payment = payment };
        var service = new PaymentService(repository);

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
}
