using System.Data;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Settings;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.Inventory;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Fulfillment;

public sealed class DeliveryArtifactSqlServerTests :
    IClassFixture<InventoryAllocationSqlServerFixture>
{
    private const string PreviousMigration =
        "20260719071615_AddFulfillmentExecutionFoundation";

    private readonly InventoryAllocationSqlServerFixture _fixture;

    public DeliveryArtifactSqlServerTests(
        InventoryAllocationSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerInventoryAllocationFact]
    public async Task MigrationCreatesArtifactTableAndCanRollbackAndReapply()
    {
        await using var context = _fixture.CreateContext();
        var migrator = context.GetService<IMigrator>();

        Assert.True(await TableExistsAsync(context, "DeliveryArtifacts"));

        try
        {
            await migrator.MigrateAsync(PreviousMigration);
            Assert.False(await TableExistsAsync(context, "DeliveryArtifacts"));
        }
        finally
        {
            await migrator.MigrateAsync();
        }

        Assert.True(await TableExistsAsync(context, "DeliveryArtifacts"));
    }

    [SqlServerInventoryAllocationFact]
    public async Task WriterPersistsOnlyProtectedValueWhenCallerSaves()
    {
        var identifiers = await SeedExecutionAsync();
        await using var context = _fixture.CreateContext();
        var protector = CreateProtector();
        var writer = new DeliveryArtifactWriter(context, protector);
        const string plaintext = "low-entropy-code-123";
        var order = OrderTestFactory.Create();
        order.Id = identifiers.OrderId;
        var execution = FulfillmentExecution.Create(
            identifiers.ExecutionId,
            order.Id);
        var attempt = execution.StartAttempt(
            identifiers.AttemptId,
            FulfillmentMode.ManualAgent,
            Timestamp(0));

        var artifact = writer.Stage(
            order,
            execution,
            attempt,
            DeliveryArtifactType.DigitalCode,
            plaintext,
            Timestamp(1));

        Assert.False(await context.DeliveryArtifacts.AnyAsync(
            candidate => candidate.Id == artifact.Id));
        await context.SaveChangesAsync();

        await using var verificationContext = _fixture.CreateContext();
        var persisted = await verificationContext.DeliveryArtifacts
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == artifact.Id);
        Assert.NotEqual(plaintext, persisted.ProtectedValue);
        Assert.DoesNotContain(
            plaintext,
            persisted.ProtectedValue,
            StringComparison.Ordinal);
        Assert.Equal(plaintext, protector.Unprotect(persisted.ProtectedValue));
    }

    [SqlServerInventoryAllocationFact]
    public async Task InvalidArtifactTypeIsRejectedByDatabase()
    {
        var identifiers = await SeedExecutionAsync();
        var artifact = CreateArtifact(identifiers);
        await using var context = _fixture.CreateContext();
        context.DeliveryArtifacts.Add(artifact);
        context.Entry(artifact)
            .Property(nameof(DeliveryArtifact.ArtifactType))
            .CurrentValue = (DeliveryArtifactType)999;

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            context.SaveChangesAsync());
    }

    [SqlServerInventoryAllocationFact]
    public async Task EveryRequiredForeignKeyRejectsMissingPrincipal()
    {
        var identifiers = await SeedExecutionAsync();
        var invalidIdentifiers = new[]
        {
            identifiers with { OrderId = Guid.NewGuid() },
            identifiers with { ExecutionId = Guid.NewGuid() },
            identifiers with { AttemptId = Guid.NewGuid() }
        };

        foreach (var invalid in invalidIdentifiers)
        {
            await using var context = _fixture.CreateContext();
            context.DeliveryArtifacts.Add(CreateArtifact(invalid));

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                context.SaveChangesAsync());
        }

        var second = await SeedExecutionAsync();
        var crossLinkedIdentifiers = new[]
        {
            new ArtifactIdentifiers(
                identifiers.OrderId,
                second.ExecutionId,
                second.AttemptId),
            new ArtifactIdentifiers(
                identifiers.OrderId,
                identifiers.ExecutionId,
                second.AttemptId)
        };

        foreach (var invalid in crossLinkedIdentifiers)
        {
            await using var context = _fixture.CreateContext();
            context.DeliveryArtifacts.Add(CreateArtifact(invalid));

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                context.SaveChangesAsync());
        }
    }

    [SqlServerInventoryAllocationFact]
    public async Task PersistedArtifactCannotBeUpdatedOrDeleted()
    {
        var identifiers = await SeedExecutionAsync();
        var artifact = CreateArtifact(identifiers);

        await using (var seedContext = _fixture.CreateContext())
        {
            seedContext.DeliveryArtifacts.Add(artifact);
            await seedContext.SaveChangesAsync();
        }

        await using (var updateContext = _fixture.CreateContext())
        {
            var persisted = await updateContext.DeliveryArtifacts.SingleAsync(
                candidate => candidate.Id == artifact.Id);
            updateContext.Entry(persisted)
                .Property(nameof(DeliveryArtifact.ProtectedValue))
                .CurrentValue = "changed-protected-value";

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                updateContext.SaveChangesAsync());
        }

        await using (var deleteContext = _fixture.CreateContext())
        {
            var persisted = await deleteContext.DeliveryArtifacts.SingleAsync(
                candidate => candidate.Id == artifact.Id);
            deleteContext.DeliveryArtifacts.Remove(persisted);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                deleteContext.SaveChangesAsync());
        }

        using (var updateContext = _fixture.CreateContext())
        {
            var persisted = updateContext.DeliveryArtifacts.Single(
                candidate => candidate.Id == artifact.Id);
            updateContext.Entry(persisted)
                .Property(nameof(DeliveryArtifact.ProtectedValue))
                .CurrentValue = "synchronous-change";

            Assert.Throws<InvalidOperationException>(() =>
                updateContext.SaveChanges());
        }

        using (var deleteContext = _fixture.CreateContext())
        {
            var persisted = deleteContext.DeliveryArtifacts.Single(
                candidate => candidate.Id == artifact.Id);
            deleteContext.DeliveryArtifacts.Remove(persisted);

            Assert.Throws<InvalidOperationException>(() =>
                deleteContext.SaveChanges());
        }

        await using var verificationContext = _fixture.CreateContext();
        Assert.True(await verificationContext.DeliveryArtifacts.AnyAsync(
            candidate => candidate.Id == artifact.Id));
    }

    private async Task<ArtifactIdentifiers> SeedExecutionAsync()
    {
        var setup = CreateSetup();
        var order = CreateOrder(setup);
        var execution = FulfillmentExecution.Create(Guid.NewGuid(), order.Id);
        var attempt = execution.StartAttempt(
            Guid.NewGuid(),
            FulfillmentMode.ManualAgent,
            Timestamp(0));

        await using var context = _fixture.CreateContext();
        context.Add(setup.User);
        context.Add(setup.Variant);
        context.Orders.Add(order);
        context.FulfillmentExecutions.Add(execution);
        await context.SaveChangesAsync();

        return new ArtifactIdentifiers(order.Id, execution.Id, attempt.Id);
    }

    private static DeliveryArtifact CreateArtifact(ArtifactIdentifiers identifiers)
    {
        return DeliveryArtifact.Create(
            Guid.NewGuid(),
            identifiers.OrderId,
            identifiers.ExecutionId,
            identifiers.AttemptId,
            DeliveryArtifactType.AccountAccess,
            "protected-value",
            new string('D', DeliveryArtifact.FingerprintLength),
            Timestamp(1));
    }

    private static DataProtectionDeliveryArtifactProtector CreateProtector()
    {
        return new DataProtectionDeliveryArtifactProtector(
            new EphemeralDataProtectionProvider(),
            Options.Create(new DeliveryArtifactProtectionOptions
            {
                FingerprintKey = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        "FEDCBA9876543210FEDCBA9876543210"))
            }));
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
            Name = $"Artifact category {suffix}",
            Slug = $"artifact-category-{suffix}",
            IsActive = true
        };
        var provider = new Provider
        {
            Name = $"Artifact provider {suffix}",
            Slug = $"artifact-provider-{suffix}",
            IsActive = true
        };
        var service = new DigitalService
        {
            CategoryId = category.Id,
            Category = category,
            ProviderId = provider.Id,
            Provider = provider,
            Name = $"Artifact service {suffix}",
            Slug = $"artifact-service-{suffix}",
            CurrencyCode = "USD",
            BasePrice = 1m,
            IsActive = true
        };
        var variant = new ServiceVariant
        {
            ServiceId = service.Id,
            Service = service,
            Name = $"Artifact variant {suffix}",
            Slug = $"artifact-variant-{suffix}",
            CurrencyCode = "USD",
            RegionCode = "GLOBAL",
            FulfillmentMode = FulfillmentMode.ManualAgent,
            BasePrice = 1m,
            IsActive = true
        };
        var user = new User
        {
            FullName = "Artifact User",
            Email = $"artifact-{suffix}@example.com",
            PasswordHash = "hash",
            IsActive = true
        };

        return new SqlSetup(user, variant);
    }

    private static DateTime Timestamp(int minute)
    {
        return new DateTime(2026, 7, 19, 10, minute, 0, DateTimeKind.Utc);
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

    private sealed record ArtifactIdentifiers(
        Guid OrderId,
        Guid ExecutionId,
        Guid AttemptId);

    private sealed record SqlSetup(User User, ServiceVariant Variant);
}
