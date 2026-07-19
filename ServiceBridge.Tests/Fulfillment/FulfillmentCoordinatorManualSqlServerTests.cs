using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Application.Payments;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Repositories;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.Inventory;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Fulfillment;

public sealed class FulfillmentCoordinatorManualSqlServerTests :
    IClassFixture<InventoryAllocationSqlServerFixture>
{
    private readonly InventoryAllocationSqlServerFixture _fixture;

    public FulfillmentCoordinatorManualSqlServerTests(
        InventoryAllocationSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerInventoryAllocationFact]
    public async Task PaymentAndPendingManualExecutionPersistInOneTransaction()
    {
        var scenario = await SeedAsync(
            OrderStatus.WaitingForPayment,
            FulfillmentMode.ManualAgent,
            includePayment: true);

        await using (var context = _fixture.CreateContext())
        {
            var service = CreatePaymentService(context);
            var result = await service.ConfirmAsync(
                new ConfirmPaymentRequest
                {
                    PaymentId = scenario.PaymentId!.Value,
                    GatewayReference = "manual-sql"
                },
                "{}");

            Assert.True(result.Success);
        }

        await using var verification = _fixture.CreateContext();
        var order = await verification.Orders.SingleAsync(x =>
            x.Id == scenario.OrderId);
        var payment = await verification.Payments.SingleAsync(x =>
            x.Id == scenario.PaymentId);
        var execution = await verification.FulfillmentExecutions
            .Include(x => x.Attempts)
            .SingleAsync(x => x.OrderId == scenario.OrderId);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(FulfillmentExecutionStatus.Pending, execution.Status);
        Assert.Empty(execution.Attempts);
    }

    [SqlServerInventoryAllocationFact]
    public async Task AssignmentStartAndCompletionPersistOneManualLifecycle()
    {
        var scenario = await SeedAsync(
            OrderStatus.Paid,
            FulfillmentMode.ManualAgent);

        await using (var assignmentContext = _fixture.CreateContext())
        {
            var service = new AdminOrderAssignmentService(
                new AdminOrderCommandRepository(assignmentContext),
                new FakeNotificationService(),
                new FakeRealtimeEventPublisher(),
                new FakeAuditLogWriter(),
                Coordinator(assignmentContext));

            var assigned = await service.AssignAsync(
                scenario.AdminId,
                scenario.OrderId,
                new AssignOrderRequest { AgentId = scenario.AgentId });
            Assert.True(assigned.Success);
        }

        await using (var agentContext = _fixture.CreateContext())
        {
            var service = CreateAgentService(agentContext);
            var started = await service.StartProcessingAsync(
                scenario.AgentId,
                scenario.OrderId,
                new AgentOrderTransitionRequest());
            var completed = await service.CompleteAsync(
                scenario.AgentId,
                scenario.OrderId,
                new AgentOrderTransitionRequest());

            Assert.True(started.Success);
            Assert.True(completed.Success);
        }

        await using var verification = _fixture.CreateContext();
        var order = await verification.Orders.SingleAsync(x =>
            x.Id == scenario.OrderId);
        var execution = await verification.FulfillmentExecutions
            .Include(x => x.Attempts)
            .SingleAsync(x => x.OrderId == scenario.OrderId);

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(FulfillmentExecutionStatus.Completed, execution.Status);
        Assert.Equal(FulfillmentAttemptStatus.Succeeded,
            Assert.Single(execution.Attempts).Status);
    }

    [SqlServerInventoryAllocationFact]
    public async Task AgentFailurePersistsTerminalExecutionWithoutRawReason()
    {
        var scenario = await SeedAsync(
            OrderStatus.Paid,
            FulfillmentMode.ManualAgent);
        await AssignAndStartAsync(scenario);

        await using (var context = _fixture.CreateContext())
        {
            var service = CreateAgentService(context);
            var result = await service.FailAsync(
                scenario.AgentId,
                scenario.OrderId,
                new FailOrderRequest { Reason = "raw provider reason" });
            Assert.True(result.Success);
        }

        await using var verification = _fixture.CreateContext();
        var order = await verification.Orders.SingleAsync(x =>
            x.Id == scenario.OrderId);
        var execution = await verification.FulfillmentExecutions
            .Include(x => x.Attempts)
            .SingleAsync(x => x.OrderId == scenario.OrderId);
        var attempt = Assert.Single(execution.Attempts);

        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.Equal(FulfillmentExecutionStatus.Failed, execution.Status);
        Assert.Equal(FulfillmentFailureDisposition.Terminal,
            attempt.FailureDisposition);
        Assert.Equal("manual_agent_failed", attempt.FailureCode?.Value);
        Assert.Null(attempt.FailureMessage);
    }

    [SqlServerInventoryAllocationFact]
    public async Task ConcurrentEnsure_IsProtectedByUniqueOrderConstraint()
    {
        var scenario = await SeedAsync(
            OrderStatus.Paid,
            FulfillmentMode.ManualAgent);
        await using var firstContext = _fixture.CreateContext();
        await using var secondContext = _fixture.CreateContext();
        var firstOrder = await firstContext.Orders
            .AsNoTracking()
            .SingleAsync(x => x.Id == scenario.OrderId);
        var secondOrder = await secondContext.Orders
            .AsNoTracking()
            .SingleAsync(x => x.Id == scenario.OrderId);

        var first = await Coordinator(firstContext)
            .EnsureForPaidOrderAsync(firstOrder);
        var second = await Coordinator(secondContext)
            .EnsureForPaidOrderAsync(secondOrder);
        Assert.True(first.Success);
        Assert.True(second.Success);

        await firstContext.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            secondContext.SaveChangesAsync());

        await using var verification = _fixture.CreateContext();
        Assert.Equal(1, await verification.FulfillmentExecutions.CountAsync(x =>
            x.OrderId == scenario.OrderId));
    }

    [SqlServerInventoryAllocationFact]
    public async Task FailedPaymentSave_RollsBackOrderPaymentAndExecution()
    {
        var scenario = await SeedAsync(
            OrderStatus.WaitingForPayment,
            FulfillmentMode.ManualAgent,
            includePayment: true);

        await using (var context = _fixture.CreateContext(
                         new ThrowingSaveChangesInterceptor()))
        {
            var service = CreatePaymentService(context);
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                service.ConfirmAsync(
                    new ConfirmPaymentRequest
                    {
                        PaymentId = scenario.PaymentId!.Value,
                        GatewayReference = "rollback"
                    },
                    "{}"));
        }

        await using var verification = _fixture.CreateContext();
        Assert.Equal(
            OrderStatus.WaitingForPayment,
            await verification.Orders
                .Where(x => x.Id == scenario.OrderId)
                .Select(x => x.Status)
                .SingleAsync());
        Assert.Equal(
            PaymentStatus.Pending,
            await verification.Payments
                .Where(x => x.Id == scenario.PaymentId)
                .Select(x => x.Status)
                .SingleAsync());
        Assert.False(await verification.FulfillmentExecutions.AnyAsync(x =>
            x.OrderId == scenario.OrderId));
    }

    [SqlServerInventoryAllocationFact]
    public async Task InventoryPaymentCreatesNoFulfillmentExecution()
    {
        var scenario = await SeedAsync(
            OrderStatus.WaitingForPayment,
            FulfillmentMode.InventoryCode,
            includePayment: true);

        await using (var context = _fixture.CreateContext())
        {
            var result = await CreatePaymentService(context).ConfirmAsync(
                new ConfirmPaymentRequest
                {
                    PaymentId = scenario.PaymentId!.Value,
                    GatewayReference = "inventory-no-stock"
                },
                "{}");
            Assert.True(result.Success);
        }

        await using var verification = _fixture.CreateContext();
        Assert.False(await verification.FulfillmentExecutions.AnyAsync(x =>
            x.OrderId == scenario.OrderId));
    }

    private async Task AssignAndStartAsync(SqlScenario scenario)
    {
        await using (var assignmentContext = _fixture.CreateContext())
        {
            var assignment = new AdminOrderAssignmentService(
                new AdminOrderCommandRepository(assignmentContext),
                new FakeNotificationService(),
                new FakeRealtimeEventPublisher(),
                new FakeAuditLogWriter(),
                Coordinator(assignmentContext));
            Assert.True((await assignment.AssignAsync(
                scenario.AdminId,
                scenario.OrderId,
                new AssignOrderRequest { AgentId = scenario.AgentId })).Success);
        }

        await using var agentContext = _fixture.CreateContext();
        Assert.True((await CreateAgentService(agentContext).StartProcessingAsync(
            scenario.AgentId,
            scenario.OrderId,
            new AgentOrderTransitionRequest())).Success);
    }

    private static PaymentService CreatePaymentService(
        ServiceBridgeDbContext context)
    {
        var notifications = new FakeNotificationService();
        return new PaymentService(
            new PaymentRepository(context),
            new FakeMockPaymentGateway(),
            new FakePaymentGatewayFactory([new FakePaymentGateway()]),
            notifications,
            new FakeRealtimeEventPublisher(),
            new FulfillInventoryCodeOrderCommand(
                new FakeInventoryCodeFulfillmentRepository(),
                notifications),
            new PaymentConfirmationTransactionManager(context),
            Coordinator(context),
            NullLogger<PaymentService>.Instance);
    }

    private static AgentOrderProcessingService CreateAgentService(
        ServiceBridgeDbContext context)
    {
        return new AgentOrderProcessingService(
            new AgentOrderCommandRepository(context),
            new FakeOrderFieldProtector(),
            new FakeNotificationService(),
            new FakeRealtimeEventPublisher(),
            Coordinator(context));
    }

    private static FulfillmentCoordinator Coordinator(
        ServiceBridgeDbContext context)
    {
        return new FulfillmentCoordinator(
            new FulfillmentExecutionRepository(context));
    }

    private async Task<SqlScenario> SeedAsync(
        OrderStatus status,
        FulfillmentMode mode,
        bool includePayment = false)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var category = new ServiceCategory
        {
            Name = $"Coordinator category {suffix}",
            Slug = $"coordinator-category-{suffix}",
            IsActive = true
        };
        var provider = new Provider
        {
            Name = $"Coordinator provider {suffix}",
            Slug = $"coordinator-provider-{suffix}",
            IsActive = true
        };
        var service = new DigitalService
        {
            Category = category,
            CategoryId = category.Id,
            Provider = provider,
            ProviderId = provider.Id,
            Name = $"Coordinator service {suffix}",
            Slug = $"coordinator-service-{suffix}",
            CurrencyCode = "USD",
            BasePrice = 1m,
            SupportsUseExistingAccount = true,
            IsActive = true
        };
        var variant = new ServiceVariant
        {
            Service = service,
            ServiceId = service.Id,
            Name = $"Coordinator variant {suffix}",
            Slug = $"coordinator-variant-{suffix}",
            CurrencyCode = "USD",
            RegionCode = "GLOBAL",
            FulfillmentMode = mode,
            BasePrice = 1m,
            IsActive = true
        };
        var customer = User("Customer", $"customer-{suffix}@example.test", UserRole.User);
        var agent = User("Agent", $"agent-{suffix}@example.test", UserRole.Agent);
        var admin = User("Admin", $"admin-{suffix}@example.test", UserRole.Admin);
        var order = OrderTestFactory.Create(mode);
        order.OrderNumber = $"CO-{suffix}"[..30];
        order.UserId = customer.Id;
        order.ServiceId = service.Id;
        order.ServiceVariantId = variant.Id;
        order.OrderType = OrderType.Purchase;
        order.Status = status;
        order.ForeignPrice = 1m;
        order.CurrencyCode = "USD";
        order.ExchangeRateSnapshot = 1m;
        order.BaseAmountIrr = 1m;
        order.FinalAmountIrr = 1m;

        Payment? payment = null;
        if (includePayment)
        {
            payment = new Payment
            {
                OrderId = order.Id,
                Order = order,
                AmountIRR = 1m,
                AmountUSD = 1m,
                ExchangeRateSnapshot = 1m
            };
            payment.RecordInitiated();
        }

        await using var context = _fixture.CreateContext();
        context.AddRange(customer, agent, admin, variant, order);
        if (payment is not null)
        {
            context.Payments.Add(payment);
        }

        await context.SaveChangesAsync();
        return new SqlScenario(
            order.Id,
            payment?.Id,
            agent.Id,
            admin.Id);
    }

    private static User User(
        string name,
        string email,
        UserRole role)
    {
        return new User
        {
            FullName = name,
            Email = email,
            PasswordHash = "test-only-hash",
            Role = role,
            IsActive = true
        };
    }

    private sealed record SqlScenario(
        Guid OrderId,
        Guid? PaymentId,
        Guid AgentId,
        Guid AdminId);

    private sealed class ThrowingSaveChangesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("Injected test save failure.");
        }
    }
}
