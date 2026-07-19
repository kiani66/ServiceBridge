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

namespace ServiceBridge.Tests.Fulfillment;

public sealed class FulfillmentManualMigrationServiceTests
{
    [Fact]
    public async Task InitialManualPayment_CreatesPendingExecutionInSingleSave()
    {
        var order = PolicyOrder(OrderStatus.WaitingForPayment);
        var payment = PaymentFor(order);
        var payments = new FakePaymentRepository { Payment = payment };
        var executions = new FakeFulfillmentExecutionRepository();
        var notifications = new FakeNotificationService();
        var service = CreatePaymentService(
            payments,
            executions,
            notifications: notifications);

        var result = await ConfirmAsync(service, payment.Id);

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(1, payments.SaveCount);
        var execution = Assert.Single(executions.Executions);
        Assert.Equal(FulfillmentExecutionStatus.Pending, execution.Status);
        Assert.Empty(execution.Attempts);
        Assert.Contains(nameof(FakeNotificationService.QueuePaymentCompletedAsync),
            notifications.QueuedTypes);
        Assert.Contains(nameof(FakeNotificationService.QueueOrderReadyForAssignmentAsync),
            notifications.QueuedTypes);
    }

    [Fact]
    public async Task CompletedCallback_RepairsMissingManualExecutionWithoutSideEffects()
    {
        var order = PolicyOrder(OrderStatus.Paid);
        var payment = PaymentFor(order);
        payment.Complete("existing", "{}", DateTime.UtcNow);
        var payments = new FakePaymentRepository { Payment = payment };
        var executions = new FakeFulfillmentExecutionRepository();
        var notifications = new FakeNotificationService();
        var realtime = new FakeRealtimeEventPublisher();
        var transactions = new FakePaymentConfirmationTransactionManager();
        var service = CreatePaymentService(
            payments,
            executions,
            notifications,
            realtime,
            transactions);

        var result = await ConfirmAsync(service, payment.Id);

        Assert.True(result.Success);
        Assert.Single(executions.Executions);
        Assert.Equal(1, payments.SaveCount);
        Assert.Equal(1, transactions.BeginCount);
        Assert.Equal(1, transactions.CommitCount);
        Assert.Empty(notifications.QueuedTypes);
        Assert.Empty(realtime.OrderUpdates);
    }

    [Fact]
    public async Task RepeatedCompletedCallback_DoesNotDuplicateExecutionOrSave()
    {
        var order = PolicyOrder(OrderStatus.Paid);
        var payment = PaymentFor(order);
        payment.Complete("existing", "{}", DateTime.UtcNow);
        var payments = new FakePaymentRepository { Payment = payment };
        var executions = new FakeFulfillmentExecutionRepository();
        executions.Executions.Add(FulfillmentExecution.Create(
            Guid.NewGuid(), order.Id));
        var service = CreatePaymentService(payments, executions);

        var result = await ConfirmAsync(service, payment.Id);

        Assert.True(result.Success);
        Assert.Single(executions.Executions);
        Assert.Equal(0, payments.SaveCount);
    }

    [Fact]
    public async Task FailedPayment_DoesNotCreateExecution()
    {
        var order = PolicyOrder(OrderStatus.WaitingForPayment);
        var payment = PaymentFor(order);
        var executions = new FakeFulfillmentExecutionRepository();
        var service = CreatePaymentService(
            new FakePaymentRepository { Payment = payment },
            executions);

        var result = await service.ConfirmAsync(
            new ConfirmPaymentRequest
            {
                PaymentId = payment.Id,
                Status = PaymentStatus.Failed,
                FailureReason = "Declined."
            },
            "{}");

        Assert.True(result.Success);
        Assert.Empty(executions.Executions);
    }

    [Fact]
    public async Task MismatchedLegacyInventoryPayment_DoesNotCreateManualExecution()
    {
        var order = FulfillmentCoordinatorTests.CreatePolicyOrder(
            FulfillmentMode.ManualAgent,
            FulfillmentMode.InventoryCode,
            OrderStatus.WaitingForPayment);
        PopulatePaymentFields(order);
        order.ServiceVariantId = Guid.NewGuid();
        var payment = PaymentFor(order);
        var executions = new FakeFulfillmentExecutionRepository();
        var service = CreatePaymentService(
            new FakePaymentRepository { Payment = payment },
            executions);

        var result = await ConfirmAsync(service, payment.Id);

        Assert.True(result.Success);
        Assert.Empty(executions.Executions);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public async Task MismatchedPolicyLegacyManualPayment_UsesLegacyPathWithoutExecution()
    {
        var order = MismatchedPolicyLegacyManualOrder(
            OrderStatus.WaitingForPayment);
        var payment = PaymentFor(order);
        var payments = new FakePaymentRepository { Payment = payment };
        var executions = new FakeFulfillmentExecutionRepository();
        var notifications = new FakeNotificationService();
        var service = CreatePaymentService(
            payments,
            executions,
            notifications: notifications);

        var result = await ConfirmAsync(service, payment.Id);

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Empty(executions.Executions);
        Assert.Equal(1, payments.SaveCount);
        Assert.Contains(nameof(FakeNotificationService.QueuePaymentCompletedAsync),
            notifications.QueuedTypes);
        Assert.Contains(nameof(FakeNotificationService.QueueOrderReadyForAssignmentAsync),
            notifications.QueuedTypes);
    }

    [Fact]
    public async Task MismatchedPolicyLegacyManualCompletedCallback_RemainsIdempotent()
    {
        var order = MismatchedPolicyLegacyManualOrder(OrderStatus.Paid);
        var payment = PaymentFor(order);
        payment.Complete("existing", "{}", DateTime.UtcNow);
        var payments = new FakePaymentRepository { Payment = payment };
        var executions = new FakeFulfillmentExecutionRepository();
        var notifications = new FakeNotificationService();
        var realtime = new FakeRealtimeEventPublisher();
        var service = CreatePaymentService(
            payments,
            executions,
            notifications,
            realtime);

        var result = await ConfirmAsync(service, payment.Id);

        Assert.True(result.Success);
        Assert.Empty(executions.Executions);
        Assert.Equal(0, payments.SaveCount);
        Assert.Empty(notifications.QueuedTypes);
        Assert.Empty(realtime.OrderUpdates);
    }

    [Fact]
    public async Task ManualAssignment_LazilyCreatesExecutionWithoutAttempt()
    {
        var order = FulfillmentCoordinatorTests.CreateHistoricalOrder(
            FulfillmentMode.ManualAgent,
            OrderStatus.Paid);
        order.OrderNumber = "ORD-MANUAL";
        var orders = new FakeAdminOrderCommandRepository { Order = order };
        var executions = new FakeFulfillmentExecutionRepository();
        var service = new AdminOrderAssignmentService(
            orders,
            new FakeNotificationService(),
            new FakeRealtimeEventPublisher(),
            new FakeAuditLogWriter(),
            new FulfillmentCoordinator(executions));

        var result = await service.AssignAsync(
            Guid.NewGuid(),
            order.Id,
            new AssignOrderRequest { AgentId = Guid.NewGuid() });

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Assigned, order.Status);
        Assert.Equal(1, orders.SaveCount);
        var execution = Assert.Single(executions.Executions);
        Assert.Equal(FulfillmentExecutionStatus.Pending, execution.Status);
        Assert.Empty(execution.Attempts);
    }

    [Fact]
    public async Task InventoryAssignment_RemainsRejectedWithoutExecution()
    {
        var order = FulfillmentCoordinatorTests.CreateHistoricalOrder(
            FulfillmentMode.InventoryCode,
            OrderStatus.Paid);
        var executions = new FakeFulfillmentExecutionRepository();
        var service = new AdminOrderAssignmentService(
            new FakeAdminOrderCommandRepository { Order = order },
            new FakeNotificationService(),
            new FakeRealtimeEventPublisher(),
            new FakeAuditLogWriter(),
            new FulfillmentCoordinator(executions));

        var result = await service.AssignAsync(
            Guid.NewGuid(),
            order.Id,
            new AssignOrderRequest { AgentId = Guid.NewGuid() });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Empty(executions.Executions);
    }

    [Fact]
    public async Task MismatchedPolicyLegacyManual_AssignmentStartAndCompleteUseLegacyPath()
    {
        var agentId = Guid.NewGuid();
        var order = MismatchedPolicyLegacyManualOrder(OrderStatus.Paid);
        order.Service = BasicService();
        var executions = new FakeFulfillmentExecutionRepository();
        var adminOrders = new FakeAdminOrderCommandRepository { Order = order };
        var assignment = new AdminOrderAssignmentService(
            adminOrders,
            new FakeNotificationService(),
            new FakeRealtimeEventPublisher(),
            new FakeAuditLogWriter(),
            new FulfillmentCoordinator(executions));

        var assigned = await assignment.AssignAsync(
            Guid.NewGuid(),
            order.Id,
            new AssignOrderRequest { AgentId = agentId });
        var agentOrders = new FakeAgentOrderCommandRepository { Order = order };
        var processing = CreateAgentService(
            agentOrders,
            executions,
            new FakeRealtimeEventPublisher());
        var started = await processing.StartProcessingAsync(
            agentId,
            order.Id,
            new AgentOrderTransitionRequest());
        var completed = await processing.CompleteAsync(
            agentId,
            order.Id,
            new AgentOrderTransitionRequest());

        Assert.True(assigned.Success);
        Assert.True(started.Success);
        Assert.True(completed.Success);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Empty(executions.Executions);
        Assert.Equal(1, adminOrders.SaveCount);
        Assert.Equal(2, agentOrders.SaveCount);
    }

    [Fact]
    public async Task MismatchedPolicyLegacyManual_StartAndFailUseLegacyPath()
    {
        var agentId = Guid.NewGuid();
        var order = MismatchedPolicyLegacyManualOrder(OrderStatus.Assigned);
        order.AssignedAgentId = agentId;
        order.Service = BasicService();
        var executions = new FakeFulfillmentExecutionRepository();
        var orders = new FakeAgentOrderCommandRepository { Order = order };
        var service = CreateAgentService(
            orders,
            executions,
            new FakeRealtimeEventPublisher());

        var started = await service.StartProcessingAsync(
            agentId,
            order.Id,
            new AgentOrderTransitionRequest());
        var failed = await service.FailAsync(
            agentId,
            order.Id,
            new FailOrderRequest { Reason = "legacy mismatch failure" });

        Assert.True(started.Success);
        Assert.True(failed.Success);
        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.Empty(executions.Executions);
        Assert.Equal(2, orders.SaveCount);
    }

    [Fact]
    public async Task StartAndComplete_MutateOrderAndExecutionTogether()
    {
        var setup = CreateAgentSetup(OrderStatus.Assigned);

        var started = await setup.Service.StartProcessingAsync(
            setup.AgentId,
            setup.Order.Id,
            new AgentOrderTransitionRequest());
        var completed = await setup.Service.CompleteAsync(
            setup.AgentId,
            setup.Order.Id,
            new AgentOrderTransitionRequest());

        Assert.True(started.Success);
        Assert.True(completed.Success);
        Assert.Equal(OrderStatus.Completed, setup.Order.Status);
        var execution = Assert.Single(setup.Executions.Executions);
        Assert.Equal(FulfillmentExecutionStatus.Completed, execution.Status);
        Assert.Equal(FulfillmentAttemptStatus.Succeeded,
            Assert.Single(execution.Attempts).Status);
        Assert.Equal(2, setup.Orders.SaveCount);
    }

    [Fact]
    public async Task StartAndFail_TerminallyFailsAttemptWithoutRawReason()
    {
        var setup = CreateAgentSetup(OrderStatus.Assigned);
        await setup.Service.StartProcessingAsync(
            setup.AgentId,
            setup.Order.Id,
            new AgentOrderTransitionRequest());

        var failed = await setup.Service.FailAsync(
            setup.AgentId,
            setup.Order.Id,
            new FailOrderRequest { Reason = "Provider included a customer secret." });

        Assert.True(failed.Success);
        Assert.Equal(OrderStatus.Failed, setup.Order.Status);
        var execution = Assert.Single(setup.Executions.Executions);
        var attempt = Assert.Single(execution.Attempts);
        Assert.Equal(FulfillmentExecutionStatus.Failed, execution.Status);
        Assert.Equal("manual_agent_failed", attempt.FailureCode?.Value);
        Assert.Null(attempt.FailureMessage);
        Assert.Equal("Provider included a customer secret.",
            Assert.Single(setup.Order.StatusHistory.Where(x =>
                x.NewStatus == OrderStatus.Failed)).Note);
    }

    [Fact]
    public async Task DeliveryValidationFailure_LeavesAttemptProcessing()
    {
        var setup = CreateAgentSetup(OrderStatus.Assigned);
        await setup.Service.StartProcessingAsync(
            setup.AgentId,
            setup.Order.Id,
            new AgentOrderTransitionRequest());
        ConfigureRequiredDelivery(setup.Order);

        var result = await setup.Service.CompleteAsync(
            setup.AgentId,
            setup.Order.Id,
            new AgentOrderTransitionRequest());

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(OrderStatus.Processing, setup.Order.Status);
        Assert.Equal(FulfillmentAttemptStatus.Processing,
            Assert.Single(Assert.Single(setup.Executions.Executions).Attempts).Status);
    }

    [Fact]
    public async Task NewSnapshottedProcessingOrderWithoutExecution_CompletionConflicts()
    {
        var setup = CreateAgentSetup(OrderStatus.Processing);

        var result = await setup.Service.CompleteAsync(
            setup.AgentId,
            setup.Order.Id,
            new AgentOrderTransitionRequest());

        Assert.False(result.Success);
        Assert.Equal(AppConstants.ErrorCodes.FulfillmentExecutionMissing,
            result.ErrorCode);
        Assert.Equal(0, setup.Orders.SaveCount);
        Assert.Empty(setup.Executions.Executions);
        Assert.Empty(setup.Notifications.QueuedTypes);
        Assert.Empty(setup.Realtime.OrderUpdates);
    }

    [Fact]
    public async Task HistoricalProcessingOrderWithoutExecution_CompletesWithoutAttempt()
    {
        var agentId = Guid.NewGuid();
        var order = FulfillmentCoordinatorTests.CreateHistoricalOrder(
            FulfillmentMode.ManualAgent,
            OrderStatus.Processing);
        order.AssignedAgentId = agentId;
        order.Service = BasicService();
        var orders = new FakeAgentOrderCommandRepository { Order = order };
        var executions = new FakeFulfillmentExecutionRepository();
        var service = CreateAgentService(
            orders,
            executions,
            new FakeRealtimeEventPublisher());

        var result = await service.CompleteAsync(
            agentId,
            order.Id,
            new AgentOrderTransitionRequest());

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Empty(executions.Executions);
        Assert.Equal(1, orders.SaveCount);
    }

    [Fact]
    public async Task SaveConflict_PublishesNoNotificationOrRealtime()
    {
        var setup = CreateAgentSetup(OrderStatus.Assigned);
        setup.Orders.SaveSucceeds = false;

        var result = await setup.Service.StartProcessingAsync(
            setup.AgentId,
            setup.Order.Id,
            new AgentOrderTransitionRequest());

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.DoesNotContain(
            nameof(FakeNotificationService.PublishQueuedRealtimeAsync),
            setup.Notifications.QueuedTypes);
        Assert.Empty(setup.Realtime.OrderUpdates);
    }

    private static PaymentService CreatePaymentService(
        FakePaymentRepository payments,
        FakeFulfillmentExecutionRepository executions,
        FakeNotificationService? notifications = null,
        FakeRealtimeEventPublisher? realtime = null,
        FakePaymentConfirmationTransactionManager? transactions = null)
    {
        notifications ??= new FakeNotificationService();
        realtime ??= new FakeRealtimeEventPublisher();
        return new PaymentService(
            payments,
            new FakeMockPaymentGateway(),
            new FakePaymentGatewayFactory([new FakePaymentGateway()]),
            notifications,
            realtime,
            new FulfillInventoryCodeOrderCommand(
                new FakeInventoryCodeFulfillmentRepository(),
                notifications),
            transactions ?? new FakePaymentConfirmationTransactionManager(),
            new FulfillmentCoordinator(executions),
            NullLogger<PaymentService>.Instance);
    }

    private static Task<Application.Results.ServiceResult> ConfirmAsync(
        PaymentService service,
        Guid paymentId)
    {
        return service.ConfirmAsync(
            new ConfirmPaymentRequest
            {
                PaymentId = paymentId,
                GatewayReference = "gateway-manual"
            },
            "{}");
    }

    private static Payment PaymentFor(Order order)
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

    private static Order PolicyOrder(OrderStatus status)
    {
        var order = FulfillmentCoordinatorTests.CreatePolicyOrder(
            FulfillmentMode.ManualAgent,
            FulfillmentMode.ManualAgent,
            status);
        PopulatePaymentFields(order);
        return order;
    }

    private static Order MismatchedPolicyLegacyManualOrder(OrderStatus status)
    {
        var order = FulfillmentCoordinatorTests.CreatePolicyOrder(
            FulfillmentMode.InventoryCode,
            FulfillmentMode.ManualAgent,
            status);
        PopulatePaymentFields(order);
        return order;
    }

    private static void PopulatePaymentFields(Order order)
    {
        order.OrderNumber = $"ORD-{Guid.NewGuid():N}";
        order.ForeignPrice = 10m;
        order.ExchangeRateSnapshot = 100_000m;
        order.FinalAmountIrr = 1_000_000m;
    }

    private static AgentSetup CreateAgentSetup(OrderStatus status)
    {
        var agentId = Guid.NewGuid();
        var order = PolicyOrder(status);
        order.AssignedAgentId = agentId;
        order.Service = BasicService();
        var orders = new FakeAgentOrderCommandRepository { Order = order };
        var executions = new FakeFulfillmentExecutionRepository();
        var realtime = new FakeRealtimeEventPublisher();
        var notifications = new FakeNotificationService();
        return new AgentSetup(
            agentId,
            order,
            CreateAgentService(orders, executions, realtime, notifications),
            orders,
            executions,
            realtime,
            notifications);
    }

    private static AgentOrderProcessingService CreateAgentService(
        FakeAgentOrderCommandRepository orders,
        FakeFulfillmentExecutionRepository executions,
        FakeRealtimeEventPublisher realtime,
        FakeNotificationService? notifications = null)
    {
        return new AgentOrderProcessingService(
            orders,
            new FakeOrderFieldProtector(),
            notifications ?? new FakeNotificationService(),
            realtime,
            new FulfillmentCoordinator(executions));
    }

    private static DigitalService BasicService()
    {
        return new DigitalService
        {
            Id = Guid.NewGuid(),
            SupportsUseExistingAccount = true
        };
    }

    private static void ConfigureRequiredDelivery(Order order)
    {
        order.FulfillmentMode = OrderFulfillmentMode.CreateNewAccount;
        order.Service.SupportsCreateNewAccount = true;
        order.Service.RequiredFields =
        [
            new ServiceRequiredField
            {
                Id = Guid.NewGuid(),
                ServiceId = order.Service.Id,
                FieldKey = "created_password",
                Label = "Created password",
                FieldType = ServiceFieldType.Password,
                FulfillmentMode = OrderFulfillmentMode.CreateNewAccount,
                Actor = CredentialActor.Agent,
                IsRequired = true,
                IsSensitive = true,
                DisplayOrder = 1
            }
        ];
    }

    private sealed record AgentSetup(
        Guid AgentId,
        Order Order,
        AgentOrderProcessingService Service,
        FakeAgentOrderCommandRepository Orders,
        FakeFulfillmentExecutionRepository Executions,
        FakeRealtimeEventPublisher Realtime,
        FakeNotificationService Notifications);
}
