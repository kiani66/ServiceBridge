using Microsoft.Extensions.Logging.Abstractions;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Application.Payments;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Payments;

public sealed class InventoryCodePaymentTests
{
    private static readonly DateTime UtcNow =
        new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task InventoryCodePaymentSuccess_CommitsOnceBeforeRealtime()
    {
        var order = CreateWaitingOrder();
        var payment = CreatePayment(order);
        var item = CreateAvailableItem(order.ServiceVariantId!.Value);
        var setup = CreateSetup(payment, nextAvailable: item);

        var result = await setup.Service.ConfirmAsync(
            CompletedRequest(payment.Id),
            "{}");

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(order.Id, item.ReservedForOrderId);
        Assert.Equal(1, setup.Payments.SaveCount);
        Assert.Equal(1, setup.Transaction.BeginCount);
        Assert.Equal(1, setup.Transaction.CommitCount);
        Assert.Equal(0, setup.Transaction.RollbackCount);
        Assert.Contains(
            nameof(FakeNotificationService.QueuePaymentCompletedAsync),
            setup.Notifications.QueuedTypes);
        Assert.Contains(
            nameof(FakeNotificationService.QueueOrderCompletedAsync),
            setup.Notifications.QueuedTypes);
        Assert.DoesNotContain(
            nameof(FakeNotificationService.QueueOrderReadyForAssignmentAsync),
            setup.Notifications.QueuedTypes);
        Assert.Equal(
            OrderStatus.Completed.ToString(),
            Assert.Single(setup.Realtime.OrderUpdates).Status);
        AssertOccursBefore(setup.Operations, "inventory.lock", "payment.save");
        AssertOccursBefore(setup.Operations, "payment.save", "transaction.commit");
        AssertOccursBefore(setup.Operations, "transaction.commit", "notification.publish");
        AssertOccursBefore(setup.Operations, "transaction.commit", "realtime.order.publish");
    }

    [Fact]
    public async Task InventoryCodePaymentWithoutStock_CompletesPaymentAndLeavesOrderPaid()
    {
        var order = CreateWaitingOrder();
        var payment = CreatePayment(order);
        var setup = CreateSetup(payment);

        var result = await setup.Service.ConfirmAsync(
            CompletedRequest(payment.Id),
            "{}");

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Null(order.AssignedAgentId);
        Assert.DoesNotContain(
            order.StatusHistory,
            history => history.NewStatus is OrderStatus.Processing or OrderStatus.Failed);
        Assert.Equal(1, setup.Payments.SaveCount);
        Assert.Equal(1, setup.Transaction.CommitCount);
        Assert.Empty(setup.Notifications.QueuedOrderCompletions);
        Assert.DoesNotContain(
            nameof(FakeNotificationService.QueueOrderReadyForAssignmentAsync),
            setup.Notifications.QueuedTypes);
    }

    [Fact]
    public async Task ManualAgentPayment_PreservesReadyForAssignmentFlow()
    {
        var order = CreateWaitingOrder(FulfillmentMode.ManualAgent);
        var payment = CreatePayment(order);
        var setup = CreateSetup(payment);

        var result = await setup.Service.ConfirmAsync(
            CompletedRequest(payment.Id),
            "{}");

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Contains(
            nameof(FakeNotificationService.QueuePaymentCompletedAsync),
            setup.Notifications.QueuedTypes);
        Assert.Contains(
            nameof(FakeNotificationService.QueueOrderReadyForAssignmentAsync),
            setup.Notifications.QueuedTypes);
        Assert.Equal(0, setup.Inventory.ExistingAllocationCalls);
        Assert.Equal(0, setup.Inventory.LockCalls);
        Assert.Equal(1, setup.Payments.SaveCount);
        Assert.Equal(1, setup.Transaction.CommitCount);
    }

    [Fact]
    public async Task DuplicateCompletedPaymentWithPaidOrder_ResumesReservedItemWithoutDuplicates()
    {
        var order = CreatePaidOrder();
        var payment = CreateCompletedPayment(order);
        var item = CreateAvailableItem(order.ServiceVariantId!.Value);
        item.ReserveFor(order.Id, UtcNow.AddMinutes(-1));
        var paymentEventCount = payment.Events.Count;
        var historyCount = order.StatusHistory.Count;
        var setup = CreateSetup(payment, existingAllocation: item);

        var result = await setup.Service.ConfirmAsync(
            CompletedRequest(payment.Id),
            "{}");

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(paymentEventCount, payment.Events.Count);
        Assert.Equal(historyCount + 2, order.StatusHistory.Count);
        Assert.DoesNotContain(
            nameof(FakeNotificationService.QueuePaymentCompletedAsync),
            setup.Notifications.QueuedTypes);
        Assert.DoesNotContain(
            nameof(FakeNotificationService.QueueOrderCompletedAsync),
            setup.Notifications.QueuedTypes);
        Assert.Equal(0, setup.Inventory.LockCalls);
        Assert.Equal(1, setup.Payments.SaveCount);
        Assert.Equal(1, setup.Transaction.CommitCount);
    }

    [Fact]
    public async Task DuplicateCompletedPaymentWithProcessingOrder_ResumesSameItemOnce()
    {
        var order = CreatePaidOrder();
        order.StartInventoryFulfillment(Guid.NewGuid());
        var payment = CreateCompletedPayment(order);
        var item = CreateAvailableItem(order.ServiceVariantId!.Value);
        item.ReserveFor(order.Id, UtcNow.AddMinutes(-1));
        var paymentEventCount = payment.Events.Count;
        var historyCount = order.StatusHistory.Count;
        var setup = CreateSetup(payment, existingAllocation: item);

        var first = await setup.Service.ConfirmAsync(
            CompletedRequest(payment.Id),
            "{}");
        var historyAfterFirst = order.StatusHistory.Count;
        var second = await setup.Service.ConfirmAsync(
            CompletedRequest(payment.Id),
            "{}");

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(paymentEventCount, payment.Events.Count);
        Assert.Equal(historyCount + 1, historyAfterFirst);
        Assert.Equal(historyAfterFirst, order.StatusHistory.Count);
        Assert.DoesNotContain(
            nameof(FakeNotificationService.QueuePaymentCompletedAsync),
            setup.Notifications.QueuedTypes);
        Assert.DoesNotContain(
            nameof(FakeNotificationService.QueueOrderCompletedAsync),
            setup.Notifications.QueuedTypes);
        Assert.Equal(0, setup.Inventory.LockCalls);
    }

    [Fact]
    public async Task DuplicateCompletedPaymentWithPaidOrder_ReconcilesDeliveredItem()
    {
        var order = CreatePaidOrder();
        var payment = CreateCompletedPayment(order);
        var item = CreateAvailableItem(order.ServiceVariantId!.Value);
        item.ReserveFor(order.Id, UtcNow.AddMinutes(-1));
        item.MarkDelivered(order.Id, UtcNow.AddSeconds(-30));
        var deliveredAt = item.DeliveredAtUtc;
        var paymentEventCount = payment.Events.Count;
        var setup = CreateSetup(payment, existingAllocation: item);

        var result = await setup.Service.ConfirmAsync(
            CompletedRequest(payment.Id),
            "{}");

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(deliveredAt, item.DeliveredAtUtc);
        Assert.Equal(paymentEventCount, payment.Events.Count);
        Assert.DoesNotContain(
            nameof(FakeNotificationService.QueuePaymentCompletedAsync),
            setup.Notifications.QueuedTypes);
        Assert.DoesNotContain(
            nameof(FakeNotificationService.QueueOrderCompletedAsync),
            setup.Notifications.QueuedTypes);
        Assert.Equal(0, setup.Inventory.LockCalls);
        Assert.Equal(1, setup.Transaction.CommitCount);
    }

    [Fact]
    public async Task TransactionSaveFailure_DoesNotCommitOrPublishMutations()
    {
        var order = CreateWaitingOrder();
        var payment = CreatePayment(order);
        var item = CreateAvailableItem(order.ServiceVariantId!.Value);
        var setup = CreateSetup(payment, nextAvailable: item);
        setup.Payments.SaveSucceeds = false;

        var result = await setup.Service.ConfirmAsync(
            CompletedRequest(payment.Id),
            "{}");

        Assert.False(result.Success);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(1, setup.Payments.SaveCount);
        Assert.Equal(0, setup.Transaction.CommitCount);
        Assert.Equal(1, setup.Transaction.RollbackCount);
        Assert.DoesNotContain("notification.publish", setup.Operations);
        Assert.DoesNotContain("realtime.order.publish", setup.Operations);
    }

    [Fact]
    public async Task RealtimeFailureAfterCommit_LeavesConfirmationSuccessful()
    {
        var order = CreateWaitingOrder();
        var payment = CreatePayment(order);
        var item = CreateAvailableItem(order.ServiceVariantId!.Value);
        var setup = CreateSetup(payment, nextAvailable: item);
        setup.Notifications.ThrowOnPublishQueuedRealtime = true;
        setup.Realtime.ThrowOnOrderUpdate = true;

        var result = await setup.Service.ConfirmAsync(
            CompletedRequest(payment.Id),
            "{}");

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(1, setup.Transaction.CommitCount);
        AssertOccursBefore(setup.Operations, "transaction.commit", "notification.publish");
        AssertOccursBefore(setup.Operations, "transaction.commit", "realtime.order.publish");
    }

    [Fact]
    public async Task GatewayVerification_CompletesBeforeTransactionBegins()
    {
        var order = CreateWaitingOrder();
        var payment = CreatePayment(order);
        var item = CreateAvailableItem(order.ServiceVariantId!.Value);
        var setup = CreateSetup(payment, nextAvailable: item);
        setup.Gateway.VerifyResult = new PaymentGatewayVerifyResult
        {
            Provider = PaymentGatewayProviders.Mock,
            IsVerified = true,
            PaymentId = payment.Id,
            Status = PaymentStatus.Completed,
            SanitizedPayload = "{}"
        };

        var result = await setup.Service.VerifyCallbackAsync(
            new PaymentGatewayVerifyRequest(
                PaymentGatewayProviders.Mock,
                $$"""{"paymentId":"{{payment.Id}}"}"""));

        Assert.True(result.Success);
        AssertOccursBefore(setup.Operations, "gateway.verify", "transaction.begin");
        Assert.Equal(1, setup.Transaction.CommitCount);
    }

    private static PaymentSetup CreateSetup(
        Payment payment,
        InventoryItem? existingAllocation = null,
        InventoryItem? nextAvailable = null)
    {
        var operations = new List<string>();
        var payments = new FakePaymentRepository
        {
            Payment = payment,
            Operations = operations
        };
        var transaction = new FakePaymentConfirmationTransactionManager(operations);
        var inventory = new FakeInventoryCodeFulfillmentRepository
        {
            ExistingAllocation = existingAllocation,
            NextAvailable = nextAvailable,
            IsTransactionActive = () => transaction.IsActive,
            Operations = operations
        };
        var notifications = new FakeNotificationService { Operations = operations };
        var realtime = new FakeRealtimeEventPublisher { Operations = operations };
        var gateway = new FakePaymentGateway { Operations = operations };
        var fulfillment = new FulfillInventoryCodeOrderCommand(
            inventory,
            notifications);
        var service = new PaymentService(
            payments,
            new FakeMockPaymentGateway(),
            new FakePaymentGatewayFactory([gateway]),
            notifications,
            realtime,
            fulfillment,
            transaction,
            NullLogger<PaymentService>.Instance);

        return new PaymentSetup(
            service,
            payments,
            inventory,
            transaction,
            notifications,
            realtime,
            gateway,
            operations);
    }

    private static Order CreateWaitingOrder(
        FulfillmentMode fulfillmentMode = FulfillmentMode.InventoryCode)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"INV-{Guid.NewGuid():N}",
            UserId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            ServiceVariantId = Guid.NewGuid(),
            ServiceFulfillmentModeSnapshot = fulfillmentMode,
            Status = OrderStatus.Created,
            CreatedAt = UtcNow.AddMinutes(-5)
        };
        order.MoveToWaitingForPayment(Guid.NewGuid(), UtcNow.AddMinutes(-4));
        return order;
    }

    private static Order CreatePaidOrder()
    {
        var order = CreateWaitingOrder();
        order.MarkPaid(Guid.NewGuid(), "Payment confirmed.");
        return order;
    }

    private static Payment CreatePayment(Order order)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            AmountIRR = 1_000_000m,
            AmountUSD = 10m,
            ExchangeRateSnapshot = 100_000m,
            CreatedAt = UtcNow.AddMinutes(-3)
        };
    }

    private static Payment CreateCompletedPayment(Order order)
    {
        var payment = CreatePayment(order);
        payment.Complete("existing", "{}", UtcNow.AddMinutes(-2));
        return payment;
    }

    private static InventoryItem CreateAvailableItem(Guid serviceVariantId)
    {
        return InventoryItem.Create(
            serviceVariantId,
            FulfillmentMode.InventoryCode,
            "protected-inventory-code",
            "inventory-code-fingerprint",
            Guid.NewGuid(),
            UtcNow.AddMinutes(-2));
    }

    private static ConfirmPaymentRequest CompletedRequest(Guid paymentId)
    {
        return new ConfirmPaymentRequest
        {
            PaymentId = paymentId,
            Status = PaymentStatus.Completed,
            GatewayReference = "gateway-confirmed"
        };
    }

    private static void AssertOccursBefore(
        IList<string> operations,
        string earlier,
        string later)
    {
        var earlierIndex = operations.IndexOf(earlier);
        var laterIndex = operations.IndexOf(later);
        Assert.True(earlierIndex >= 0, $"Missing operation: {earlier}");
        Assert.True(laterIndex >= 0, $"Missing operation: {later}");
        Assert.True(
            earlierIndex < laterIndex,
            $"Expected {earlier} before {later}: {string.Join(", ", operations)}");
    }

    private sealed record PaymentSetup(
        PaymentService Service,
        FakePaymentRepository Payments,
        FakeInventoryCodeFulfillmentRepository Inventory,
        FakePaymentConfirmationTransactionManager Transaction,
        FakeNotificationService Notifications,
        FakeRealtimeEventPublisher Realtime,
        FakePaymentGateway Gateway,
        List<string> Operations);
}
