using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Repositories;
using ServiceBridge.Tests.Inventory;
using Xunit;

namespace ServiceBridge.Tests.Payments;

public sealed class PaymentRepositoryInventoryConfirmationSqlServerTests :
    IClassFixture<InventoryAllocationSqlServerFixture>
{
    private readonly InventoryAllocationSqlServerFixture _fixture;

    public PaymentRepositoryInventoryConfirmationSqlServerTests(
        InventoryAllocationSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerInventoryAllocationFact]
    public async Task ConfirmationRead_WithoutTransaction_IsRejected()
    {
        var scenario = await SeedScenarioAsync();
        await using var context = _fixture.CreateContext();
        var repository = new PaymentRepository(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.GetPaymentWithOrderForConfirmationAsync(
                scenario.PaymentId));

        Assert.Equal(
            "Payment confirmation requires an active database transaction.",
            exception.Message);
    }

    [SqlServerInventoryAllocationFact]
    public async Task ConfirmationRead_LocksOrderBeforeLoadingAggregate()
    {
        var scenario = await SeedScenarioAsync();
        var commands = new ConfirmationCommandSequenceInterceptor();
        await using var context = _fixture.CreateContext(commands);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var repository = new PaymentRepository(context);

        var payment = await repository.GetPaymentWithOrderForConfirmationAsync(
            scenario.PaymentId);

        Assert.NotNull(payment);
        Assert.True(commands.OrderLockIndex >= 0);
        Assert.True(commands.AggregateLoadIndex > commands.OrderLockIndex);
        await transaction.RollbackAsync();
    }

    [SqlServerInventoryAllocationFact]
    public async Task ConfirmationRead_WaitingBehindFulfillmentReturnsFreshOrder()
    {
        var scenario = await SeedScenarioAsync();
        var lockAttempt = new OrderLockAttemptInterceptor();
        await using var confirmationContext = _fixture.CreateContext(lockAttempt);
        var stalePayment = await confirmationContext.Payments
            .Include(candidate => candidate.Order)
            .Include(candidate => candidate.Events)
            .SingleAsync(candidate => candidate.Id == scenario.PaymentId);
        Assert.Equal(OrderStatus.Paid, stalePayment.Order.Status);

        await using var fulfillmentContext = _fixture.CreateContext();
        await using var fulfillmentTransaction =
            await fulfillmentContext.Database.BeginTransactionAsync();
        var lockedOrder = await new AdminOrderCommandRepository(fulfillmentContext)
            .LockOrderForInventoryFulfillmentAsync(scenario.OrderId);
        Assert.NotNull(lockedOrder);
        lockedOrder.StartInventoryFulfillment(
            scenario.AdminId,
            "Concurrent fulfillment test lock.");
        await fulfillmentContext.SaveChangesAsync();

        await using var confirmationTransaction =
            await confirmationContext.Database.BeginTransactionAsync();
        var confirmationTask = new PaymentRepository(confirmationContext)
            .GetPaymentWithOrderForConfirmationAsync(scenario.PaymentId);

        await lockAttempt.WaitUntilOrderLockRequestedAsync();
        await fulfillmentTransaction.CommitAsync();

        var freshPayment = await confirmationTask.WaitAsync(
            TimeSpan.FromSeconds(30));

        Assert.NotNull(freshPayment);
        Assert.NotSame(stalePayment, freshPayment);
        Assert.NotSame(stalePayment.Order, freshPayment.Order);
        Assert.Equal(OrderStatus.Processing, freshPayment.Order.Status);
        await confirmationTransaction.RollbackAsync();
    }

    private async Task<ConfirmationScenario> SeedScenarioAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var admin = new User
        {
            FullName = "Confirmation lock admin",
            Email = $"confirmation-lock-admin-{suffix}@example.test",
            PasswordHash = "test-only-hash",
            Role = UserRole.Admin,
            CreatedAt = now.AddMinutes(-10)
        };
        var customer = new User
        {
            FullName = "Confirmation lock customer",
            Email = $"confirmation-lock-customer-{suffix}@example.test",
            PasswordHash = "test-only-hash",
            Role = UserRole.User,
            CreatedAt = now.AddMinutes(-10)
        };
        var category = new ServiceCategory
        {
            Name = $"Confirmation lock {suffix}",
            Slug = $"confirmation-lock-{suffix}",
            IsActive = true,
            CreatedAt = now.AddMinutes(-10)
        };
        var provider = new Provider
        {
            Name = $"Confirmation lock provider {suffix}",
            Slug = $"confirmation-lock-provider-{suffix}",
            IsActive = true,
            CreatedAt = now.AddMinutes(-10)
        };
        var service = new DigitalService
        {
            CategoryId = category.Id,
            Category = category,
            ProviderId = provider.Id,
            Provider = provider,
            Name = $"Confirmation lock service {suffix}",
            Slug = $"confirmation-lock-service-{suffix}",
            CurrencyCode = "USD",
            BasePrice = 1m,
            IsActive = true,
            CreatedAt = now.AddMinutes(-10)
        };
        var variant = new ServiceVariant
        {
            ServiceId = service.Id,
            Service = service,
            Name = $"Confirmation lock variant {suffix}",
            Slug = $"confirmation-lock-variant-{suffix}",
            CurrencyCode = "USD",
            RegionCode = "GLOBAL",
            FulfillmentMode = FulfillmentMode.InventoryCode,
            BasePrice = 1m,
            IsActive = true,
            CreatedAt = now.AddMinutes(-10)
        };
        var order = new Order
        {
            OrderNumber = $"CONFIRM-{suffix[..8]}",
            UserId = customer.Id,
            User = customer,
            ServiceId = service.Id,
            Service = service,
            ServiceVariantId = variant.Id,
            ServiceVariant = variant,
            OrderType = OrderType.Purchase,
            FulfillmentMode = OrderFulfillmentMode.UseExistingAccount,
            ServiceFulfillmentModeSnapshot = FulfillmentMode.InventoryCode,
            Status = OrderStatus.Created,
            ForeignPrice = 1m,
            CurrencyCode = "USD",
            ExchangeRateSnapshot = 1m,
            MarginPercentSnapshot = 0m,
            BaseAmountIrr = 1m,
            ProfitAmountIrr = 0m,
            FinalAmountIrr = 1m,
            CreatedAt = now.AddMinutes(-10)
        };
        order.MoveToWaitingForPayment(customer.Id, now.AddMinutes(-9));
        var payment = new Payment
        {
            OrderId = order.Id,
            Order = order,
            AmountIRR = order.FinalAmountIrr,
            AmountUSD = order.ForeignPrice,
            ExchangeRateSnapshot = order.ExchangeRateSnapshot,
            CreatedAt = now.AddMinutes(-8)
        };
        payment.RecordInitiated();
        payment.Complete(
            "confirmation-lock-completed",
            null,
            now.AddMinutes(-7));
        order.MarkPaid(payment.Id, "Confirmation lock test payment.");

        await using var context = _fixture.CreateContext();
        context.AddRange(
            admin,
            customer,
            category,
            provider,
            service,
            variant,
            order,
            payment);
        await context.SaveChangesAsync();

        return new ConfirmationScenario(admin.Id, order.Id, payment.Id);
    }

    private sealed class ConfirmationCommandSequenceInterceptor :
        DbCommandInterceptor
    {
        private int _commandIndex;

        public int OrderLockIndex { get; private set; } = -1;
        public int AggregateLoadIndex { get; private set; } = -1;

        public override ValueTask<InterceptionResult<DbDataReader>>
            ReaderExecutingAsync(
                DbCommand command,
                CommandEventData eventData,
                InterceptionResult<DbDataReader> result,
                CancellationToken cancellationToken = default)
        {
            var index = _commandIndex++;
            if (IsOrderLock(command.CommandText))
            {
                OrderLockIndex = index;
            }
            else if (command.CommandText.Contains(
                         "[PaymentEvents]",
                         StringComparison.OrdinalIgnoreCase))
            {
                AggregateLoadIndex = index;
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed class OrderLockAttemptInterceptor : DbCommandInterceptor
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
        private readonly TaskCompletionSource _orderLockRequested =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitUntilOrderLockRequestedAsync() =>
            _orderLockRequested.Task.WaitAsync(Timeout);

        public override ValueTask<InterceptionResult<DbDataReader>>
            ReaderExecutingAsync(
                DbCommand command,
                CommandEventData eventData,
                InterceptionResult<DbDataReader> result,
                CancellationToken cancellationToken = default)
        {
            if (IsOrderLock(command.CommandText))
            {
                _orderLockRequested.TrySetResult();
            }

            return ValueTask.FromResult(result);
        }
    }

    private static bool IsOrderLock(string commandText)
    {
        return commandText.Contains(
            "FROM [Orders] WITH (UPDLOCK, ROWLOCK)",
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ConfirmationScenario(
        Guid AdminId,
        Guid OrderId,
        Guid PaymentId);
}
