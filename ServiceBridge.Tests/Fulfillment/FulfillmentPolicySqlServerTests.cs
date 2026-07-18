using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Tests.Inventory;
using Xunit;

namespace ServiceBridge.Tests.Fulfillment;

public sealed class FulfillmentPolicySqlServerTests :
    IClassFixture<InventoryAllocationSqlServerFixture>
{
    private const string PreviousMigration =
        "20260714110840_AddOrderServiceFulfillmentModeSnapshot";

    private readonly InventoryAllocationSqlServerFixture _fixture;

    public FulfillmentPolicySqlServerTests(
        InventoryAllocationSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerInventoryAllocationFact]
    public async Task DuplicateServiceVariantVersion_IsRejectedBySqlServer()
    {
        var serviceVariant = CreateServiceVariant();
        var first = FulfillmentPolicy.Create(
            serviceVariant.Id,
            1,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.AccountAccess);
        var duplicate = FulfillmentPolicy.Create(
            serviceVariant.Id,
            1,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.AccountAccess);

        await using var context = _fixture.CreateContext();
        context.ServiceVariants.Add(serviceVariant);
        context.FulfillmentPolicies.AddRange(first, duplicate);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            context.SaveChangesAsync());
    }

    [SqlServerInventoryAllocationFact]
    public async Task DifferentVersionsForSameServiceVariant_AreAcceptedBySqlServer()
    {
        var serviceVariant = CreateServiceVariant();
        var first = CreatePolicy(serviceVariant.Id, 1);
        var second = CreatePolicy(serviceVariant.Id, 2);

        await using var context = _fixture.CreateContext();
        context.ServiceVariants.Add(serviceVariant);
        context.FulfillmentPolicies.AddRange(first, second);

        await context.SaveChangesAsync();

        Assert.Equal(
            2,
            await context.FulfillmentPolicies.CountAsync(policy =>
                policy.ServiceVariantId == serviceVariant.Id));
    }

    [SqlServerInventoryAllocationFact]
    public async Task SameVersionForDifferentServiceVariants_IsAcceptedBySqlServer()
    {
        var firstVariant = CreateServiceVariant();
        var secondVariant = CreateServiceVariant();
        var first = CreatePolicy(firstVariant.Id, 1);
        var second = CreatePolicy(secondVariant.Id, 1);

        await using var context = _fixture.CreateContext();
        context.ServiceVariants.AddRange(firstVariant, secondVariant);
        context.FulfillmentPolicies.AddRange(first, second);

        await context.SaveChangesAsync();

        Assert.Equal(
            2,
            await context.FulfillmentPolicies.CountAsync(policy =>
                policy.Id == first.Id || policy.Id == second.Id));
    }

    [SqlServerInventoryAllocationFact]
    public async Task ReferencedServiceVariant_CannotBeDeletedBySqlServer()
    {
        var serviceVariant = CreateServiceVariant();
        var policy = CreatePolicy(serviceVariant.Id, 1);

        await using (var seedContext = _fixture.CreateContext())
        {
            seedContext.ServiceVariants.Add(serviceVariant);
            seedContext.FulfillmentPolicies.Add(policy);
            await seedContext.SaveChangesAsync();
        }

        await using var deleteContext = _fixture.CreateContext();
        var persistedVariant = await deleteContext.ServiceVariants
            .SingleAsync(variant => variant.Id == serviceVariant.Id);
        deleteContext.ServiceVariants.Remove(persistedVariant);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            deleteContext.SaveChangesAsync());
    }

    [SqlServerInventoryAllocationFact]
    public async Task Migration_CanBeRolledBackAndReapplied()
    {
        await using var context = _fixture.CreateContext();
        var migrator = context.GetService<IMigrator>();

        Assert.True(await FulfillmentPoliciesTableExistsAsync(context));

        try
        {
            await migrator.MigrateAsync(PreviousMigration);
            Assert.False(await FulfillmentPoliciesTableExistsAsync(context));
        }
        finally
        {
            await migrator.MigrateAsync();
        }

        Assert.True(await FulfillmentPoliciesTableExistsAsync(context));
    }

    private static FulfillmentPolicy CreatePolicy(
        Guid serviceVariantId,
        int version)
    {
        return FulfillmentPolicy.Create(
            serviceVariantId,
            version,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.AccountAccess);
    }

    private static async Task<bool> FulfillmentPoliciesTableExistsAsync(
        DbContext context)
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
                WHERE object_id = OBJECT_ID(N'[dbo].[FulfillmentPolicies]', N'U');
                """;

            return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static ServiceVariant CreateServiceVariant()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var category = new ServiceCategory
        {
            Name = $"Fulfillment policy {suffix}",
            Slug = $"fulfillment-policy-{suffix}",
            IsActive = true
        };
        var provider = new Provider
        {
            Name = $"Fulfillment policy provider {suffix}",
            Slug = $"fulfillment-policy-provider-{suffix}",
            IsActive = true
        };
        var service = new DigitalService
        {
            CategoryId = category.Id,
            Category = category,
            ProviderId = provider.Id,
            Provider = provider,
            Name = $"Fulfillment policy service {suffix}",
            Slug = $"fulfillment-policy-service-{suffix}",
            CurrencyCode = "USD",
            BasePrice = 1m,
            IsActive = true
        };

        return new ServiceVariant
        {
            ServiceId = service.Id,
            Service = service,
            Name = $"Fulfillment policy variant {suffix}",
            Slug = $"fulfillment-policy-variant-{suffix}",
            CurrencyCode = "USD",
            RegionCode = "GLOBAL",
            FulfillmentMode = FulfillmentMode.ManualAgent,
            BasePrice = 1m,
            IsActive = true
        };
    }
}
