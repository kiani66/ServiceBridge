using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.ValueObjects;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Tests.Inventory;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Fulfillment;

public sealed class FulfillmentExecutionSqlServerTests :
    IClassFixture<InventoryAllocationSqlServerFixture>
{
    private const string PreviousMigration =
        "20260718134135_AddOrderFulfillmentSnapshot";

    private readonly InventoryAllocationSqlServerFixture _fixture;

    public FulfillmentExecutionSqlServerTests(
        InventoryAllocationSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerInventoryAllocationFact]
    public async Task MigrationCreatesExecutionAndAttemptTables()
    {
        await using var context = _fixture.CreateContext();

        Assert.True(await TableExistsAsync(context, "FulfillmentExecutions"));
        Assert.True(await TableExistsAsync(context, "FulfillmentAttempts"));
    }

    [SqlServerInventoryAllocationFact]
    public async Task OneExecutionPerOrderIsEnforced()
    {
        var order = await SeedOrdersAsync(1);
        await using var context = _fixture.CreateContext();
        context.FulfillmentExecutions.AddRange(
            FulfillmentExecution.Create(Guid.NewGuid(), order[0].Id),
            FulfillmentExecution.Create(Guid.NewGuid(), order[0].Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        await using var verificationContext = _fixture.CreateContext();
        Assert.False(await verificationContext.FulfillmentExecutions.AnyAsync(
            execution => execution.OrderId == order[0].Id));
    }

    [SqlServerInventoryAllocationFact]
    public async Task DifferentOrdersCanHaveIndependentExecutions()
    {
        var orders = await SeedOrdersAsync(2);
        await using var context = _fixture.CreateContext();
        context.FulfillmentExecutions.AddRange(
            FulfillmentExecution.Create(Guid.NewGuid(), orders[0].Id),
            FulfillmentExecution.Create(Guid.NewGuid(), orders[1].Id));

        await context.SaveChangesAsync();

        Assert.Equal(
            2,
            await context.FulfillmentExecutions.CountAsync(execution =>
                execution.OrderId == orders[0].Id ||
                execution.OrderId == orders[1].Id));
    }

    [SqlServerInventoryAllocationFact]
    public async Task DuplicateAttemptNumberIsRejected()
    {
        var orders = await SeedOrdersAsync(1);
        var execution = FulfillmentExecution.Create(Guid.NewGuid(), orders[0].Id);
        var firstId = Guid.NewGuid();
        execution.StartAttempt(
            firstId,
            FulfillmentMode.ManualAgent,
            Timestamp(0));
        execution.FailAttempt(
            firstId,
            FulfillmentFailureDisposition.Retryable,
            FailureCode(),
            null,
            Timestamp(1));
        var second = execution.StartAttempt(
            Guid.NewGuid(),
            FulfillmentMode.ManualAgent,
            Timestamp(2));

        await using var context = _fixture.CreateContext();
        context.FulfillmentExecutions.Add(execution);
        context.Entry(second)
            .Property(nameof(FulfillmentAttempt.AttemptNumber))
            .CurrentValue = 1;

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SqlServerInventoryAllocationFact]
    public async Task ValidSuccessRetryableAndTerminalRowsPersist()
    {
        var orders = await SeedOrdersAsync(3);
        var succeeded = FulfillmentExecution.Create(Guid.NewGuid(), orders[0].Id);
        var succeededAttemptId = Guid.NewGuid();
        succeeded.StartAttempt(
            succeededAttemptId,
            FulfillmentMode.ManualAgent,
            Timestamp(0));
        succeeded.CompleteAttempt(succeededAttemptId, Timestamp(1));

        var retryable = FulfillmentExecution.Create(Guid.NewGuid(), orders[1].Id);
        var retryableAttemptId = Guid.NewGuid();
        retryable.StartAttempt(
            retryableAttemptId,
            FulfillmentMode.ExternalAutomation,
            Timestamp(0));
        retryable.FailAttempt(
            retryableAttemptId,
            FulfillmentFailureDisposition.Retryable,
            FailureCode(),
            "temporary",
            Timestamp(1));

        var terminal = FulfillmentExecution.Create(Guid.NewGuid(), orders[2].Id);
        var terminalAttemptId = Guid.NewGuid();
        terminal.StartAttempt(
            terminalAttemptId,
            FulfillmentMode.InstructionOnly,
            Timestamp(0));
        terminal.FailAttempt(
            terminalAttemptId,
            FulfillmentFailureDisposition.Terminal,
            FailureCode(),
            null,
            Timestamp(1));

        await using (var context = _fixture.CreateContext())
        {
            context.FulfillmentExecutions.AddRange(succeeded, retryable, terminal);
            await context.SaveChangesAsync();
        }

        await using var queryContext = _fixture.CreateContext();
        var attempts = await queryContext.FulfillmentAttempts
            .Where(attempt =>
                attempt.FulfillmentExecutionId == succeeded.Id ||
                attempt.FulfillmentExecutionId == retryable.Id ||
                attempt.FulfillmentExecutionId == terminal.Id)
            .OrderBy(attempt => attempt.FulfillmentExecutionId)
            .ToArrayAsync();

        Assert.Equal(3, attempts.Length);
        Assert.Contains(attempts, attempt =>
            attempt.Status == FulfillmentAttemptStatus.Succeeded &&
            attempt.FailureCode == null);
        Assert.Contains(attempts, attempt =>
            attempt.FailureDisposition == FulfillmentFailureDisposition.Retryable &&
            attempt.FailureCode == FailureCode());
        Assert.Contains(attempts, attempt =>
            attempt.FailureDisposition == FulfillmentFailureDisposition.Terminal &&
            attempt.FailureCode == FailureCode());
    }

    [SqlServerInventoryAllocationFact]
    public async Task ProcessingAttemptPersistsWithoutCompletionOrFailureMetadata()
    {
        var orders = await SeedOrdersAsync(1);
        var execution = FulfillmentExecution.Create(Guid.NewGuid(), orders[0].Id);
        var attemptId = Guid.NewGuid();
        execution.StartAttempt(
            attemptId,
            FulfillmentMode.ManualAgent,
            Timestamp(0));

        await using (var context = _fixture.CreateContext())
        {
            context.FulfillmentExecutions.Add(execution);
            await context.SaveChangesAsync();
        }

        await using var queryContext = _fixture.CreateContext();
        var persistedExecution = await queryContext.FulfillmentExecutions
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == execution.Id);
        var persistedAttempt = await queryContext.FulfillmentAttempts
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == attemptId);

        Assert.Equal(FulfillmentExecutionStatus.Processing, persistedExecution.Status);
        Assert.NotEmpty(persistedExecution.RowVersion);
        Assert.Equal(FulfillmentAttemptStatus.Processing, persistedAttempt.Status);
        Assert.Null(persistedAttempt.CompletedAtUtc);
        Assert.Null(persistedAttempt.FailureDisposition);
        Assert.Null(persistedAttempt.FailureCode);
        Assert.Null(persistedAttempt.FailureMessage);
    }

    [SqlServerInventoryAllocationFact]
    public async Task InvalidPartialAttemptStatesAreRejected()
    {
        await AssertInvalidAttemptRejectedAsync(
            execution => execution.StartAttempt(
                Guid.NewGuid(),
                FulfillmentMode.ManualAgent,
                Timestamp(0)),
            (context, attempt) => context.Entry(attempt)
                .Property(nameof(FulfillmentAttempt.CompletedAtUtc))
                .CurrentValue = Timestamp(1));

        await AssertInvalidAttemptRejectedAsync(
            CreateSucceededAttempt,
            (context, attempt) => context.Entry(attempt)
                .Property(nameof(FulfillmentAttempt.FailureCode))
                .CurrentValue = FailureCode());

        await AssertInvalidAttemptRejectedAsync(
            CreateTerminalAttempt,
            (context, attempt) => context.Entry(attempt)
                .Property(nameof(FulfillmentAttempt.FailureCode))
                .CurrentValue = null);

        await AssertInvalidAttemptRejectedAsync(
            CreateTerminalAttempt,
            (context, attempt) => context.Entry(attempt)
                .Property(nameof(FulfillmentAttempt.FailureCode))
                .CurrentValue = EmptyFailureCode());
    }

    [SqlServerInventoryAllocationFact]
    public async Task InvalidEnumNumericValuesAreRejected()
    {
        await AssertInvalidExecutionRejectedAsync((context, execution) =>
            context.Entry(execution)
                .Property(nameof(FulfillmentExecution.Status))
                .CurrentValue = (FulfillmentExecutionStatus)999);

        await AssertInvalidAttemptRejectedAsync(
            execution => execution.StartAttempt(
                Guid.NewGuid(),
                FulfillmentMode.ManualAgent,
                Timestamp(0)),
            (context, attempt) => context.Entry(attempt)
                .Property(nameof(FulfillmentAttempt.FulfillmentMode))
                .CurrentValue = (FulfillmentMode)999);

        await AssertInvalidAttemptRejectedAsync(
            CreateSucceededAttempt,
            (context, attempt) => context.Entry(attempt)
                .Property(nameof(FulfillmentAttempt.Status))
                .CurrentValue = (FulfillmentAttemptStatus)999);

        await AssertInvalidAttemptRejectedAsync(
            CreateTerminalAttempt,
            (context, attempt) => context.Entry(attempt)
                .Property(nameof(FulfillmentAttempt.FailureDisposition))
                .CurrentValue = (FulfillmentFailureDisposition)999);
    }

    [SqlServerInventoryAllocationFact]
    public async Task ReferencedOrderAndExecutionCannotBeDeleted()
    {
        var orders = await SeedOrdersAsync(2);
        var executionWithoutAttempt = FulfillmentExecution.Create(
            Guid.NewGuid(),
            orders[0].Id);
        var executionWithAttempt = FulfillmentExecution.Create(
            Guid.NewGuid(),
            orders[1].Id);
        executionWithAttempt.StartAttempt(
            Guid.NewGuid(),
            FulfillmentMode.ManualAgent,
            Timestamp(0));

        await using (var seedContext = _fixture.CreateContext())
        {
            seedContext.FulfillmentExecutions.AddRange(
                executionWithoutAttempt,
                executionWithAttempt);
            await seedContext.SaveChangesAsync();
        }

        await using (var orderContext = _fixture.CreateContext())
        {
            var order = await orderContext.Orders.SingleAsync(
                candidate => candidate.Id == orders[0].Id);
            orderContext.Orders.Remove(order);
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                orderContext.SaveChangesAsync());
        }

        await using (var executionContext = _fixture.CreateContext())
        {
            var execution = await executionContext.FulfillmentExecutions
                .SingleAsync(candidate => candidate.Id == executionWithAttempt.Id);
            executionContext.FulfillmentExecutions.Remove(execution);
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                executionContext.SaveChangesAsync());
        }
    }

    [SqlServerInventoryAllocationFact]
    public async Task AttemptsInEveryStatusCannotBeDeleted()
    {
        var orders = await SeedOrdersAsync(3);
        var processing = FulfillmentExecution.Create(Guid.NewGuid(), orders[0].Id);
        var processingAttempt = processing.StartAttempt(
            Guid.NewGuid(),
            FulfillmentMode.ManualAgent,
            Timestamp(0));

        var succeeded = FulfillmentExecution.Create(Guid.NewGuid(), orders[1].Id);
        var succeededAttempt = CreateSucceededAttempt(succeeded);

        var failed = FulfillmentExecution.Create(Guid.NewGuid(), orders[2].Id);
        var failedAttempt = CreateTerminalAttempt(failed);

        await using (var seedContext = _fixture.CreateContext())
        {
            seedContext.FulfillmentExecutions.AddRange(processing, succeeded, failed);
            await seedContext.SaveChangesAsync();
        }

        var attemptIds = new[]
        {
            processingAttempt.Id,
            succeededAttempt.Id,
            failedAttempt.Id
        };
        foreach (var attemptId in attemptIds)
        {
            await using var context = _fixture.CreateContext();
            var attempt = await context.FulfillmentAttempts.SingleAsync(
                candidate => candidate.Id == attemptId);
            context.FulfillmentAttempts.Remove(attempt);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                context.SaveChangesAsync());
        }

        await using var verificationContext = _fixture.CreateContext();
        Assert.Equal(
            3,
            await verificationContext.FulfillmentAttempts.CountAsync(attempt =>
                attemptIds.Contains(attempt.Id)));
    }

    [SqlServerInventoryAllocationFact]
    public async Task TerminalAttemptsRejectPostTerminalModification()
    {
        var orders = await SeedOrdersAsync(2);
        var succeeded = FulfillmentExecution.Create(Guid.NewGuid(), orders[0].Id);
        var succeededAttempt = CreateSucceededAttempt(succeeded);
        var failed = FulfillmentExecution.Create(Guid.NewGuid(), orders[1].Id);
        var failedAttempt = CreateTerminalAttempt(failed);

        await using (var seedContext = _fixture.CreateContext())
        {
            seedContext.FulfillmentExecutions.AddRange(succeeded, failed);
            await seedContext.SaveChangesAsync();
        }

        foreach (var attemptId in new[] { succeededAttempt.Id, failedAttempt.Id })
        {
            await using var context = _fixture.CreateContext();
            var attempt = await context.FulfillmentAttempts.SingleAsync(
                candidate => candidate.Id == attemptId);
            context.Entry(attempt)
                .Property(nameof(FulfillmentAttempt.FailureMessage))
                .CurrentValue = "post-terminal mutation";

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                context.SaveChangesAsync());
        }

        await using var verificationContext = _fixture.CreateContext();
        Assert.All(
            await verificationContext.FulfillmentAttempts
                .Where(attempt =>
                    attempt.Id == succeededAttempt.Id ||
                    attempt.Id == failedAttempt.Id)
                .AsNoTracking()
                .ToArrayAsync(),
            attempt => Assert.Null(attempt.FailureMessage));
    }

    [SqlServerInventoryAllocationFact]
    public async Task StaleExecutionWriteRaisesConcurrencyExceptionAndPreservesWinningState()
    {
        var orders = await SeedOrdersAsync(1);
        var execution = FulfillmentExecution.Create(Guid.NewGuid(), orders[0].Id);
        var attemptId = Guid.NewGuid();
        execution.StartAttempt(
            attemptId,
            FulfillmentMode.ManualAgent,
            Timestamp(0));

        await using (var seedContext = _fixture.CreateContext())
        {
            seedContext.FulfillmentExecutions.Add(execution);
            await seedContext.SaveChangesAsync();
        }

        await using var winningContext = _fixture.CreateContext();
        await using var staleContext = _fixture.CreateContext();
        var winningExecution = await winningContext.FulfillmentExecutions
            .Include(candidate => candidate.Attempts)
            .SingleAsync(candidate => candidate.Id == execution.Id);
        var staleExecution = await staleContext.FulfillmentExecutions
            .Include(candidate => candidate.Attempts)
            .SingleAsync(candidate => candidate.Id == execution.Id);

        winningExecution.CompleteAttempt(attemptId, Timestamp(1));
        await winningContext.SaveChangesAsync();

        staleExecution.FailAttempt(
            attemptId,
            FulfillmentFailureDisposition.Terminal,
            FailureCode(),
            "stale failure",
            Timestamp(2));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            staleContext.SaveChangesAsync());

        await using var verificationContext = _fixture.CreateContext();
        var persisted = await verificationContext.FulfillmentExecutions
            .Include(candidate => candidate.Attempts)
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == execution.Id);
        var persistedAttempt = Assert.Single(persisted.Attempts);

        Assert.Equal(FulfillmentExecutionStatus.Completed, persisted.Status);
        Assert.Equal(1, persisted.LastAttemptNumber);
        Assert.Equal(FulfillmentAttemptStatus.Succeeded, persistedAttempt.Status);
        Assert.Equal(Timestamp(1), persistedAttempt.CompletedAtUtc);
        Assert.Null(persistedAttempt.FailureDisposition);
        Assert.Null(persistedAttempt.FailureCode);
        Assert.Null(persistedAttempt.FailureMessage);
    }

    [SqlServerInventoryAllocationFact]
    public async Task MigrationCanBeRolledBackAndReapplied()
    {
        await using var context = _fixture.CreateContext();
        var migrator = context.GetService<IMigrator>();

        Assert.True(await TableExistsAsync(context, "FulfillmentExecutions"));

        try
        {
            await migrator.MigrateAsync(PreviousMigration);
            Assert.False(await TableExistsAsync(context, "FulfillmentExecutions"));
            Assert.False(await TableExistsAsync(context, "FulfillmentAttempts"));
        }
        finally
        {
            await migrator.MigrateAsync();
        }

        Assert.True(await TableExistsAsync(context, "FulfillmentExecutions"));
        Assert.True(await TableExistsAsync(context, "FulfillmentAttempts"));
    }

    private async Task<Order[]> SeedOrdersAsync(int count)
    {
        var setup = CreateSetup();
        var orders = Enumerable.Range(0, count)
            .Select(_ => CreateOrder(setup))
            .ToArray();

        await using var context = _fixture.CreateContext();
        context.Add(setup.User);
        context.Add(setup.Variant);
        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();
        return orders;
    }

    private async Task AssertInvalidExecutionRejectedAsync(
        Action<ServiceBridgeDbContext, FulfillmentExecution> makeInvalid)
    {
        var orders = await SeedOrdersAsync(1);
        var execution = FulfillmentExecution.Create(Guid.NewGuid(), orders[0].Id);

        await using var context = _fixture.CreateContext();
        context.FulfillmentExecutions.Add(execution);
        makeInvalid(context, execution);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private async Task AssertInvalidAttemptRejectedAsync(
        Func<FulfillmentExecution, FulfillmentAttempt> createAttempt,
        Action<ServiceBridgeDbContext, FulfillmentAttempt> makeInvalid)
    {
        var orders = await SeedOrdersAsync(1);
        var execution = FulfillmentExecution.Create(Guid.NewGuid(), orders[0].Id);
        var attempt = createAttempt(execution);

        await using var context = _fixture.CreateContext();
        context.FulfillmentExecutions.Add(execution);
        makeInvalid(context, attempt);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static FulfillmentAttempt CreateSucceededAttempt(
        FulfillmentExecution execution)
    {
        var attemptId = Guid.NewGuid();
        var attempt = execution.StartAttempt(
            attemptId,
            FulfillmentMode.ManualAgent,
            Timestamp(0));
        execution.CompleteAttempt(attemptId, Timestamp(1));
        return attempt;
    }

    private static FulfillmentAttempt CreateTerminalAttempt(
        FulfillmentExecution execution)
    {
        var attemptId = Guid.NewGuid();
        var attempt = execution.StartAttempt(
            attemptId,
            FulfillmentMode.ManualAgent,
            Timestamp(0));
        execution.FailAttempt(
            attemptId,
            FulfillmentFailureDisposition.Terminal,
            FailureCode(),
            null,
            Timestamp(1));
        return attempt;
    }

    private static Order CreateOrder(SqlSetup setup)
    {
        var order = OrderTestFactory.Create();
        order.OrderNumber = $"SB-{Guid.NewGuid():N}"[..30];
        order.UserId = setup.User.Id;
        order.ServiceId = setup.Variant.ServiceId;
        order.ServiceVariantId = setup.Variant.Id;
        order.OrderType = OrderType.Purchase;
        order.FulfillmentMode = OrderFulfillmentMode.UseExistingAccount;
        order.Status = OrderStatus.WaitingForPayment;
        order.ForeignPrice = 1m;
        order.CurrencyCode = "USD";
        order.ExchangeRateSnapshot = 1m;
        order.MarginPercentSnapshot = 0m;
        order.BaseAmountIrr = 1m;
        order.ProfitAmountIrr = 0m;
        order.FinalAmountIrr = 1m;
        return order;
    }

    private static SqlSetup CreateSetup()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var category = new ServiceCategory
        {
            Name = $"Execution category {suffix}",
            Slug = $"execution-category-{suffix}",
            IsActive = true
        };
        var provider = new Provider
        {
            Name = $"Execution provider {suffix}",
            Slug = $"execution-provider-{suffix}",
            IsActive = true
        };
        var service = new DigitalService
        {
            CategoryId = category.Id,
            Category = category,
            ProviderId = provider.Id,
            Provider = provider,
            Name = $"Execution service {suffix}",
            Slug = $"execution-service-{suffix}",
            CurrencyCode = "USD",
            BasePrice = 1m,
            IsActive = true
        };
        var variant = new ServiceVariant
        {
            ServiceId = service.Id,
            Service = service,
            Name = $"Execution variant {suffix}",
            Slug = $"execution-variant-{suffix}",
            CurrencyCode = "USD",
            RegionCode = "GLOBAL",
            FulfillmentMode = FulfillmentMode.ManualAgent,
            BasePrice = 1m,
            IsActive = true
        };
        var user = new User
        {
            FullName = "Execution User",
            Email = $"execution-{suffix}@example.com",
            PasswordHash = "hash",
            IsActive = true
        };

        return new SqlSetup(user, variant);
    }

    private static FulfillmentFailureCode FailureCode()
    {
        return FulfillmentFailureCode.Create("Fulfillment.Unavailable");
    }

    private static FulfillmentFailureCode EmptyFailureCode()
    {
        var constructor = typeof(FulfillmentFailureCode)
            .GetConstructors(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)
            .Single(candidate =>
                candidate.GetParameters().Length == 1 &&
                candidate.GetParameters()[0].ParameterType == typeof(string));
        return (FulfillmentFailureCode)constructor.Invoke(new object[] { string.Empty });
    }

    private static DateTime Timestamp(int minute)
    {
        return new DateTime(2026, 7, 19, 8, minute, 0, DateTimeKind.Utc);
    }

    private static async Task<bool> TableExistsAsync(
        DbContext context,
        string tableName)
    {
        await context.Database.OpenConnectionAsync();

        try
        {
            await using var command = context.Database
                .GetDbConnection()
                .CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sys.tables
                WHERE object_id = OBJECT_ID(@tableName, N'U');
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = $"[dbo].[{tableName}]";
            command.Parameters.Add(parameter);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private sealed record SqlSetup(User User, ServiceVariant Variant);
}
