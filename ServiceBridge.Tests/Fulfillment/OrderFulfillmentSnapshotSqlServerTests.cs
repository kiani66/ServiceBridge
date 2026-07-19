using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Orders;
using ServiceBridge.Tests.Inventory;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Fulfillment;

public sealed class OrderFulfillmentSnapshotSqlServerTests :
    IClassFixture<InventoryAllocationSqlServerFixture>
{
    private const string PreviousMigration =
        "20260718115805_AddFulfillmentPolicyFoundation";

    private readonly InventoryAllocationSqlServerFixture _fixture;

    public OrderFulfillmentSnapshotSqlServerTests(
        InventoryAllocationSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerInventoryAllocationFact]
    public async Task CompleteSnapshot_IsPersistedAndUnaffectedByLaterPolicyVersions()
    {
        var setup = CreateSetup();
        var snapshot = Snapshot(setup.Policy);
        var order = CreateOrder(setup, snapshot);

        await using (var seedContext = _fixture.CreateContext())
        {
            AddSetup(seedContext, setup);
            seedContext.Orders.Add(order);
            await seedContext.SaveChangesAsync();
        }

        await using (var policyContext = _fixture.CreateContext())
        {
            policyContext.FulfillmentPolicies.Add(FulfillmentPolicy.Create(
                setup.Variant.Id,
                2,
                ProcurementStrategy.PreStockedInventory,
                FulfillmentMode.InventoryCode,
                DeliveryArtifactType.DigitalCode));
            await policyContext.SaveChangesAsync();
        }

        await using var queryContext = _fixture.CreateContext();
        var persisted = await queryContext.Orders
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == order.Id);

        Assert.Equal(setup.Policy.Id, persisted.FulfillmentPolicyId);
        Assert.Equal(1, persisted.FulfillmentPolicyVersionSnapshot);
        Assert.Equal(ProcurementStrategy.OnDemandManual, persisted.ProcurementStrategySnapshot);
        Assert.Equal(FulfillmentMode.ManualAgent, persisted.PolicyFulfillmentModeSnapshot);
        Assert.Equal(FulfillmentMode.ManualAgent, persisted.ServiceFulfillmentModeSnapshot);
        Assert.Equal(DeliveryArtifactType.AccountAccess, persisted.DeliveryArtifactTypeSnapshot);
    }

    [SqlServerInventoryAllocationFact]
    public async Task HistoricalOrder_WithoutPolicySnapshot_RemainsValid()
    {
        var setup = CreateSetup();
        var order = CreateOrder(setup, snapshot: null);

        await using var context = _fixture.CreateContext();
        AddSetup(context, setup);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var persisted = await context.Orders
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == order.Id);
        Assert.Null(persisted.FulfillmentPolicyId);
        Assert.Null(persisted.FulfillmentPolicyVersionSnapshot);
        Assert.Null(persisted.ProcurementStrategySnapshot);
        Assert.Null(persisted.PolicyFulfillmentModeSnapshot);
        Assert.Null(persisted.DeliveryArtifactTypeSnapshot);
    }

    [SqlServerInventoryAllocationFact]
    public async Task EveryPartialSnapshotShape_IsRejectedAndRollsBackEntireSave()
    {
        var nullableSnapshotProperties = new[]
        {
            nameof(Order.FulfillmentPolicyId),
            nameof(Order.FulfillmentPolicyVersionSnapshot),
            nameof(Order.ProcurementStrategySnapshot),
            nameof(Order.PolicyFulfillmentModeSnapshot),
            nameof(Order.DeliveryArtifactTypeSnapshot)
        };

        foreach (var propertyName in nullableSnapshotProperties)
        {
            await AssertInvalidSnapshotRejectedAsync((context, order) =>
                context.Entry(order).Property(propertyName).CurrentValue = null);
        }
    }

    [SqlServerInventoryAllocationFact]
    public async Task NonPositivePolicyVersion_IsRejectedByConstraint()
    {
        await AssertInvalidSnapshotRejectedAsync((context, order) =>
            context.Entry(order)
                .Property(nameof(Order.FulfillmentPolicyVersionSnapshot))
                .CurrentValue = 0);
    }

    [SqlServerInventoryAllocationFact]
    public async Task EveryInvalidSnapshotEnum_IsRejectedByConstraint()
    {
        var invalidValues = new (string PropertyName, object Value)[]
        {
            (nameof(Order.ProcurementStrategySnapshot), (ProcurementStrategy)999),
            (nameof(Order.PolicyFulfillmentModeSnapshot), (FulfillmentMode)999),
            (nameof(Order.DeliveryArtifactTypeSnapshot), (DeliveryArtifactType)999)
        };

        foreach (var invalidValue in invalidValues)
        {
            await AssertInvalidSnapshotRejectedAsync((context, order) =>
                context.Entry(order)
                    .Property(invalidValue.PropertyName)
                    .CurrentValue = invalidValue.Value);
        }
    }

    [SqlServerInventoryAllocationFact]
    public async Task UnknownPolicyId_IsRejectedByForeignKey()
    {
        await AssertInvalidSnapshotRejectedAsync((context, order) =>
            context.Entry(order)
                .Property(nameof(Order.FulfillmentPolicyId))
                .CurrentValue = Guid.Empty);
    }

    [SqlServerInventoryAllocationFact]
    public async Task DifferentOrders_CanSnapshotDifferentVersionsForSameVariant()
    {
        var setup = CreateSetup();
        var secondPolicy = FulfillmentPolicy.Create(
            setup.Variant.Id,
            2,
            ProcurementStrategy.PreStockedInventory,
            FulfillmentMode.InventoryCode,
            DeliveryArtifactType.DigitalCode);
        var firstOrder = CreateOrder(setup, Snapshot(setup.Policy));
        var secondOrder = CreateOrder(setup, Snapshot(secondPolicy));

        await using var context = _fixture.CreateContext();
        AddSetup(context, setup);
        context.FulfillmentPolicies.Add(secondPolicy);
        context.Orders.AddRange(firstOrder, secondOrder);
        await context.SaveChangesAsync();

        var versions = await context.Orders
            .Where(order => order.Id == firstOrder.Id || order.Id == secondOrder.Id)
            .OrderBy(order => order.FulfillmentPolicyVersionSnapshot)
            .Select(order => order.FulfillmentPolicyVersionSnapshot)
            .ToArrayAsync();
        Assert.Equal(new int?[] { 1, 2 }, versions);
    }

    [SqlServerInventoryAllocationFact]
    public async Task ReferencedPolicy_CannotBeDeleted()
    {
        var setup = CreateSetup();
        var order = CreateOrder(setup, Snapshot(setup.Policy));

        await using (var seedContext = _fixture.CreateContext())
        {
            AddSetup(seedContext, setup);
            seedContext.Orders.Add(order);
            await seedContext.SaveChangesAsync();
        }

        await using var deleteContext = _fixture.CreateContext();
        var policy = await deleteContext.FulfillmentPolicies
            .SingleAsync(candidate => candidate.Id == setup.Policy.Id);
        deleteContext.FulfillmentPolicies.Remove(policy);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            deleteContext.SaveChangesAsync());
    }

    [SqlServerInventoryAllocationFact]
    public async Task Migration_CanBeRolledBackAndReapplied()
    {
        await using var context = _fixture.CreateContext();
        var migrator = context.GetService<IMigrator>();

        Assert.True(await SnapshotColumnsExistAsync(context));

        try
        {
            await migrator.MigrateAsync(PreviousMigration);
            Assert.False(await SnapshotColumnsExistAsync(context));
        }
        finally
        {
            await migrator.MigrateAsync();
        }

        Assert.True(await SnapshotColumnsExistAsync(context));
    }

    private static void AddSetup(
        DbContext context,
        SnapshotSqlSetup setup)
    {
        context.Add(setup.User);
        context.Add(setup.Variant);
        context.Add(setup.Policy);
    }

    private static Order CreateOrder(
        SnapshotSqlSetup setup,
        OrderFulfillmentPolicySnapshot? snapshot)
    {
        var order = snapshot == null
            ? OrderTestFactory.Create()
            : Order.Create(snapshot, FulfillmentMode.ManualAgent);
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

    private static OrderFulfillmentPolicySnapshot Snapshot(
        FulfillmentPolicy policy)
    {
        return OrderFulfillmentPolicySnapshot.Create(
            policy.Id,
            policy.Version,
            policy.ProcurementStrategy,
            policy.FulfillmentMode,
            policy.DeliveryArtifactType);
    }

    private static SnapshotSqlSetup CreateSetup()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var category = new ServiceCategory
        {
            Name = $"Snapshot category {suffix}",
            Slug = $"snapshot-category-{suffix}",
            IsActive = true
        };
        var provider = new Provider
        {
            Name = $"Snapshot provider {suffix}",
            Slug = $"snapshot-provider-{suffix}",
            IsActive = true
        };
        var service = new DigitalService
        {
            CategoryId = category.Id,
            Category = category,
            ProviderId = provider.Id,
            Provider = provider,
            Name = $"Snapshot service {suffix}",
            Slug = $"snapshot-service-{suffix}",
            CurrencyCode = "USD",
            BasePrice = 1m,
            IsActive = true
        };
        var variant = new ServiceVariant
        {
            ServiceId = service.Id,
            Service = service,
            Name = $"Snapshot variant {suffix}",
            Slug = $"snapshot-variant-{suffix}",
            CurrencyCode = "USD",
            RegionCode = "GLOBAL",
            FulfillmentMode = FulfillmentMode.ManualAgent,
            BasePrice = 1m,
            IsActive = true
        };
        var policy = FulfillmentPolicy.Create(
            variant.Id,
            1,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.AccountAccess);
        var user = new User
        {
            FullName = "Snapshot User",
            Email = $"snapshot-{suffix}@example.com",
            PasswordHash = "hash",
            IsActive = true
        };

        return new SnapshotSqlSetup(user, variant, policy);
    }

    private async Task AssertInvalidSnapshotRejectedAsync(
        Action<DbContext, Order> makeInvalid)
    {
        var setup = CreateSetup();
        var valid = CreateOrder(setup, Snapshot(setup.Policy));
        var invalid = CreateOrder(setup, Snapshot(setup.Policy));

        await using (var context = _fixture.CreateContext())
        {
            AddSetup(context, setup);
            context.Orders.AddRange(valid, invalid);
            makeInvalid(context, invalid);

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                context.SaveChangesAsync());
        }

        await using var verificationContext = _fixture.CreateContext();
        Assert.False(await verificationContext.Orders.AnyAsync(candidate =>
            candidate.Id == valid.Id || candidate.Id == invalid.Id));
    }

    private static async Task<bool> SnapshotColumnsExistAsync(DbContext context)
    {
        await context.Database.OpenConnectionAsync();

        try
        {
            await using var command = context.Database
                .GetDbConnection()
                .CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[Orders]', N'U')
                  AND name IN (
                      N'FulfillmentPolicyId',
                      N'FulfillmentPolicyVersionSnapshot',
                      N'ProcurementStrategySnapshot',
                      N'PolicyFulfillmentModeSnapshot',
                      N'DeliveryArtifactTypeSnapshot');
                """;

            return Convert.ToInt32(await command.ExecuteScalarAsync()) == 5;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private sealed record SnapshotSqlSetup(
        User User,
        ServiceVariant Variant,
        FulfillmentPolicy Policy);
}
