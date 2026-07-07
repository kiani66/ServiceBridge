using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Refunds;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Refunds;

public class RefundRequestCommandTests
{
    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.WaitingForPayment)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task CreateAsync_WhenOrderStatusIsInvalid_ReturnsConflict(
        OrderStatus status)
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, status);
        var repository = new FakeRefundRequestRepository { Order = order };
        var command = new CreateRefundRequest(
            repository,
            new FakeNotificationService());

        var result = await command.ExecuteAsync(
            userId,
            order.Id,
            new CreateRefundRequestRequest { Reason = "Service could not be used." });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(0, repository.AddCount);
        Assert.Equal(0, repository.SaveCount);
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Assigned)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Failed)]
    public async Task CreateAsync_WhenOrderStatusIsEligible_CreatesRefundAndAuditLog(
        OrderStatus status)
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, status);
        var notifications = new FakeNotificationService();
        var repository = new FakeRefundRequestRepository { Order = order };
        var command = new CreateRefundRequest(repository, notifications);

        var result = await command.ExecuteAsync(
            userId,
            order.Id,
            new CreateRefundRequestRequest { Reason = "The service failed." });

        Assert.True(result.Success);
        Assert.NotNull(repository.RefundRequest);
        Assert.Equal(RefundStatus.Requested, repository.RefundRequest.Status);
        Assert.Equal(order.FinalAmountIrr, repository.RefundRequest.Amount);
        Assert.Equal(1, repository.SaveCount);
        Assert.Contains(
            nameof(FakeNotificationService.QueueRefundRequestedForAdminsAsync),
            notifications.QueuedTypes);

        var audit = Assert.Single(repository.RefundRequest.StatusHistory);
        Assert.Null(audit.PreviousStatus);
        Assert.Equal(RefundStatus.Requested, audit.NewStatus);
        Assert.Equal(userId, audit.ActorId);
    }

    [Fact]
    public async Task CreateAsync_UsesPaymentAmountWhenOrderAmountIsHigher()
    {
        var userId = Guid.NewGuid();
        var paymentAmount = 1250000m;
        var order = CreateOrder(
            userId,
            OrderStatus.Failed,
            finalAmountIrr: 1750000m,
            paymentAmountIrr: paymentAmount);
        var repository = new FakeRefundRequestRepository { Order = order };
        var command = new CreateRefundRequest(
            repository,
            new FakeNotificationService());

        var result = await command.ExecuteAsync(
            userId,
            order.Id,
            new CreateRefundRequestRequest { Reason = "The service failed." });

        Assert.True(result.Success);
        Assert.NotNull(repository.RefundRequest);
        Assert.Equal(paymentAmount, repository.RefundRequest.Amount);
        Assert.True(repository.RefundRequest.Amount <= order.PaymentRecords.Single().AmountIRR);
    }

    [Fact]
    public async Task CreateAsync_WhenActiveRefundExists_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, OrderStatus.Failed);
        var repository = new FakeRefundRequestRepository
        {
            Order = order,
            ActiveRefundExists = true
        };
        var command = new CreateRefundRequest(
            repository,
            new FakeNotificationService());

        var result = await command.ExecuteAsync(
            userId,
            order.Id,
            new CreateRefundRequestRequest { Reason = "The service failed." });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(0, repository.AddCount);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ApproveAsync_WhenRequested_ApprovesAndWritesAuditLog()
    {
        var adminId = Guid.NewGuid();
        var refundRequest = CreateRefundRequestEntity();
        var notifications = new FakeNotificationService();
        var repository = new FakeRefundRequestRepository
        {
            RefundRequest = refundRequest
        };
        var command = new ApproveRefundRequest(repository, notifications);

        var result = await command.ExecuteAsync(
            adminId,
            refundRequest.Id,
            new ReviewRefundRequestRequest { AdminNote = "Approved by support." });

        Assert.True(result.Success);
        Assert.Equal(RefundStatus.Approved, refundRequest.Status);
        Assert.Equal(adminId, refundRequest.ReviewedByAdminId);
        Assert.Equal(1, repository.SaveCount);
        Assert.Contains(
            nameof(FakeNotificationService.QueueRefundApprovedForUserAsync),
            notifications.QueuedTypes);
        Assert.Contains(refundRequest.StatusHistory, x =>
            x.PreviousStatus == RefundStatus.Requested &&
            x.NewStatus == RefundStatus.Approved &&
            x.ActorId == adminId);
    }

    [Fact]
    public async Task ApproveAsync_DoesNotMarkPaymentAsRefunded()
    {
        var adminId = Guid.NewGuid();
        var payment = CreateCompletedPayment(Guid.NewGuid(), 1250000m);
        var refundRequest = CreateRefundRequestEntity(payment);
        var repository = new FakeRefundRequestRepository
        {
            RefundRequest = refundRequest
        };
        var command = new ApproveRefundRequest(
            repository,
            new FakeNotificationService());

        var result = await command.ExecuteAsync(
            adminId,
            refundRequest.Id,
            new ReviewRefundRequestRequest { AdminNote = "Approved for manual processing." });

        Assert.True(result.Success);
        Assert.Equal(RefundStatus.Approved, refundRequest.Status);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Null(payment.RefundedAt);
    }

    [Fact]
    public async Task RejectAsync_WhenRequested_RejectsAndWritesAuditLog()
    {
        var adminId = Guid.NewGuid();
        var refundRequest = CreateRefundRequestEntity();
        var notifications = new FakeNotificationService();
        var repository = new FakeRefundRequestRepository
        {
            RefundRequest = refundRequest
        };
        var command = new RejectRefundRequest(repository, notifications);

        var result = await command.ExecuteAsync(
            adminId,
            refundRequest.Id,
            new ReviewRefundRequestRequest { AdminNote = "Policy does not allow it." });

        Assert.True(result.Success);
        Assert.Equal(RefundStatus.Rejected, refundRequest.Status);
        Assert.Equal(adminId, refundRequest.ReviewedByAdminId);
        Assert.Equal(1, repository.SaveCount);
        Assert.Contains(
            nameof(FakeNotificationService.QueueRefundRejectedForUserAsync),
            notifications.QueuedTypes);
        Assert.Contains(refundRequest.StatusHistory, x =>
            x.PreviousStatus == RefundStatus.Requested &&
            x.NewStatus == RefundStatus.Rejected &&
            x.ActorId == adminId);
    }

    [Fact]
    public async Task ApproveAsync_WhenAlreadyApproved_ReturnsConflict()
    {
        var adminId = Guid.NewGuid();
        var refundRequest = CreateRefundRequestEntity();
        refundRequest.Approve(adminId, "First approval.", DateTime.UtcNow);
        var repository = new FakeRefundRequestRepository
        {
            RefundRequest = refundRequest
        };
        var command = new ApproveRefundRequest(
            repository,
            new FakeNotificationService());

        var result = await command.ExecuteAsync(
            Guid.NewGuid(),
            refundRequest.Id,
            new ReviewRefundRequestRequest { AdminNote = "Second approval." });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(0, repository.SaveCount);
    }

    private static Order CreateOrder(
        Guid userId,
        OrderStatus status,
        decimal finalAmountIrr = 1250000m,
        decimal paymentAmountIrr = 1250000m)
    {
        var orderId = Guid.NewGuid();
        var payment = CreateCompletedPayment(orderId, paymentAmountIrr);

        return new Order
        {
            Id = orderId,
            UserId = userId,
            OrderNumber = "SB-1001",
            Status = status,
            FinalAmountIrr = finalAmountIrr,
            PaymentRecords = [payment]
        };
    }

    private static Payment CreateCompletedPayment(Guid orderId, decimal amountIrr)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            AmountIRR = amountIrr,
            AmountUSD = 10m,
            ExchangeRateSnapshot = 125000m,
            CreatedAt = DateTime.UtcNow
        };
        payment.Complete("ref-1", null, DateTime.UtcNow);

        return payment;
    }

    private static RefundRequest CreateRefundRequestEntity(Payment? payment = null)
    {
        var userId = Guid.NewGuid();
        var orderId = payment?.OrderId ?? Guid.NewGuid();
        payment ??= CreateCompletedPayment(orderId, 1250000m);
        var request = RefundRequest.Create(
            orderId,
            payment.Id,
            userId,
            payment.AmountIRR,
            "The service failed.",
            DateTime.UtcNow);
        request.Order = new Order
        {
            Id = orderId,
            UserId = userId,
            OrderNumber = "SB-1001",
            Status = OrderStatus.Failed
        };
        request.Payment = payment;

        return request;
    }
}
