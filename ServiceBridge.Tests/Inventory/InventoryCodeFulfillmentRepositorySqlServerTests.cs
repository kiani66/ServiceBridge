using Microsoft.EntityFrameworkCore;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Repositories;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Inventory;

public sealed class InventoryCodeFulfillmentRepositorySqlServerTests :
    IClassFixture<InventoryAllocationSqlServerFixture>
{
    private readonly InventoryAllocationSqlServerFixture _fixture;

    public InventoryCodeFulfillmentRepositorySqlServerTests(
        InventoryAllocationSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerInventoryAllocationFact]
    public async Task LockNextAvailableAsync_WithoutTransaction_IsRejected()
    {
        await using var context = _fixture.CreateContext();
        var repository = new InventoryCodeFulfillmentRepository(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.LockNextAvailableAsync(Guid.NewGuid()));

        Assert.Equal(
            "LockNextAvailableAsync requires an active database transaction.",
            exception.Message);
    }

    [SqlServerInventoryAllocationFact]
    public async Task LockNextAvailableAsync_SelectsOldestThenLowestId()
    {
        var now = DateTime.UtcNow;
        var lowerTieId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var higherTieId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var oldestId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var scenario = await SeedScenarioAsync(
            new ItemSeed(false, now.AddMinutes(-1), lowerTieId),
            new ItemSeed(false, now.AddMinutes(-1), higherTieId),
            new ItemSeed(false, now.AddMinutes(-2), oldestId));

        await using (var context = _fixture.CreateContext())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var repository = new InventoryCodeFulfillmentRepository(context);
            var selected = await repository.LockNextAvailableAsync(scenario.VariantId);

            Assert.NotNull(selected);
            Assert.Equal(oldestId, selected.Id);
            selected.Disable(scenario.AdminId, DateTime.UtcNow);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using (var context = _fixture.CreateContext())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var repository = new InventoryCodeFulfillmentRepository(context);
            var selected = await repository.LockNextAvailableAsync(scenario.VariantId);

            Assert.NotNull(selected);
            Assert.Equal(lowerTieId, selected.Id);
            await transaction.RollbackAsync();
        }
    }

    [SqlServerInventoryAllocationFact]
    public async Task LockNextAvailableAsync_NeverSelectsAnotherVariant()
    {
        var scenario = await SeedScenarioAsync(
            new ItemSeed(false, DateTime.UtcNow.AddMinutes(-1), null),
            new ItemSeed(true, DateTime.UtcNow, null));

        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var repository = new InventoryCodeFulfillmentRepository(context);

        var selected = await repository.LockNextAvailableAsync(scenario.VariantId);

        Assert.NotNull(selected);
        Assert.Equal(scenario.VariantId, selected.ServiceVariantId);
        Assert.False(context.ChangeTracker.HasChanges());
        await transaction.RollbackAsync();
    }

    [SqlServerInventoryAllocationFact]
    public async Task LockNextAvailableAsync_WithoutStock_ReturnsNullWithoutChanges()
    {
        var scenario = await SeedScenarioAsync();

        await using var context = _fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var repository = new InventoryCodeFulfillmentRepository(context);

        var selected = await repository.LockNextAvailableAsync(scenario.VariantId);

        Assert.Null(selected);
        Assert.False(context.ChangeTracker.HasChanges());
        await transaction.RollbackAsync();
    }

    [SqlServerInventoryAllocationFact]
    public async Task GetExistingAllocationAsync_ReturnsReservedAndDeliveredOnlyForOwningOrder()
    {
        var scenario = await SeedScenarioAsync(
            new ItemSeed(false, DateTime.UtcNow.AddMinutes(-1), null),
            new ItemSeed(false, DateTime.UtcNow, null));

        await using (var context = _fixture.CreateContext())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var first = await context.InventoryItems.FindAsync(scenario.ItemIds[0]);
            var second = await context.InventoryItems.FindAsync(scenario.ItemIds[1]);
            first!.ReserveFor(scenario.Order1Id, DateTime.UtcNow);
            second!.ReserveFor(scenario.Order2Id, DateTime.UtcNow);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new InventoryCodeFulfillmentRepository(context);
            var reserved = await repository.GetExistingAllocationAsync(scenario.Order1Id);
            var anotherOrder = await repository.GetExistingAllocationAsync(Guid.NewGuid());

            Assert.NotNull(reserved);
            Assert.Equal(InventoryItemStatus.Reserved, reserved.Status);
            Assert.Equal(scenario.ItemIds[0], reserved.Id);
            Assert.Null(anotherOrder);
        }

        await using (var context = _fixture.CreateContext())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var item = await context.InventoryItems.FindAsync(scenario.ItemIds[0]);
            item!.MarkDelivered(scenario.Order1Id, DateTime.UtcNow);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new InventoryCodeFulfillmentRepository(context);
            var delivered = await repository.GetExistingAllocationAsync(scenario.Order1Id);

            Assert.NotNull(delivered);
            Assert.Equal(InventoryItemStatus.Delivered, delivered.Status);
            Assert.Equal(scenario.ItemIds[0], delivered.Id);
        }
    }

    [SqlServerInventoryAllocationFact]
    public async Task TwoOrdersCompetingForOneItem_OnlyFirstTransactionCanSelectIt()
    {
        var scenario = await SeedScenarioAsync(
            new ItemSeed(false, DateTime.UtcNow, null));
        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        await using var transactionA = await contextA.Database.BeginTransactionAsync();
        await using var transactionB = await contextB.Database.BeginTransactionAsync();
        var repositoryA = new InventoryCodeFulfillmentRepository(contextA);
        var repositoryB = new InventoryCodeFulfillmentRepository(contextB);

        var first = await repositoryA.LockNextAvailableAsync(scenario.VariantId);
        var second = await repositoryB.LockNextAvailableAsync(scenario.VariantId);

        Assert.NotNull(first);
        Assert.Null(second);

        first.ReserveFor(scenario.Order1Id, DateTime.UtcNow);
        await contextA.SaveChangesAsync();
        await transactionA.CommitAsync();
        await transactionB.RollbackAsync();

        await using var verificationContext = _fixture.CreateContext();
        var allocated = await verificationContext.InventoryItems
            .SingleAsync(item => item.Id == first.Id);
        Assert.Equal(InventoryItemStatus.Reserved, allocated.Status);
        Assert.Equal(scenario.Order1Id, allocated.ReservedForOrderId);
    }

    [SqlServerInventoryAllocationFact]
    public async Task TwoOrdersCompetingForTwoItems_ReadPastSelectsDistinctRows()
    {
        var scenario = await SeedScenarioAsync(
            new ItemSeed(false, DateTime.UtcNow.AddMinutes(-1), null),
            new ItemSeed(false, DateTime.UtcNow, null));
        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        await using var transactionA = await contextA.Database.BeginTransactionAsync();
        await using var transactionB = await contextB.Database.BeginTransactionAsync();
        var repositoryA = new InventoryCodeFulfillmentRepository(contextA);
        var repositoryB = new InventoryCodeFulfillmentRepository(contextB);

        var first = await repositoryA.LockNextAvailableAsync(scenario.VariantId);
        var second = await repositoryB.LockNextAvailableAsync(scenario.VariantId);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first.Id, second.Id);

        first.ReserveFor(scenario.Order1Id, DateTime.UtcNow);
        second.ReserveFor(scenario.Order2Id, DateTime.UtcNow);
        await contextA.SaveChangesAsync();
        await contextB.SaveChangesAsync();
        await transactionA.CommitAsync();
        await transactionB.CommitAsync();

        await using var verificationContext = _fixture.CreateContext();
        var allocations = await verificationContext.InventoryItems
            .Where(item => scenario.ItemIds.Contains(item.Id))
            .OrderBy(item => item.ReservedForOrderId)
            .ToArrayAsync();
        Assert.Equal(2, allocations.Length);
        Assert.Equal(2, allocations.Select(item => item.ReservedForOrderId).Distinct().Count());
    }

    [SqlServerInventoryAllocationFact]
    public async Task RolledBackReservation_ReleasesLockAndMutation()
    {
        var scenario = await SeedScenarioAsync(
            new ItemSeed(false, DateTime.UtcNow, null));

        await using (var context = _fixture.CreateContext())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var repository = new InventoryCodeFulfillmentRepository(context);
            var selected = await repository.LockNextAvailableAsync(scenario.VariantId);
            selected!.ReserveFor(scenario.Order1Id, DateTime.UtcNow);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var laterContext = _fixture.CreateContext();
        await using var laterTransaction = await laterContext.Database.BeginTransactionAsync();
        var laterRepository = new InventoryCodeFulfillmentRepository(laterContext);
        var selectedAgain = await laterRepository.LockNextAvailableAsync(scenario.VariantId);

        Assert.NotNull(selectedAgain);
        Assert.Equal(scenario.ItemIds[0], selectedAgain.Id);
        Assert.Equal(InventoryItemStatus.Available, selectedAgain.Status);
        Assert.Null(selectedAgain.ReservedForOrderId);
        await laterTransaction.RollbackAsync();
    }

    [SqlServerInventoryAllocationFact]
    public async Task ReservedForOrderUniqueIndex_RejectsTwoItemsForSameOrder()
    {
        var scenario = await SeedScenarioAsync(
            new ItemSeed(false, DateTime.UtcNow.AddMinutes(-1), null),
            new ItemSeed(false, DateTime.UtcNow, null));

        await using (var context = _fixture.CreateContext())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var items = await context.InventoryItems
                .Where(item => scenario.ItemIds.Contains(item.Id))
                .ToArrayAsync();
            foreach (var item in items)
            {
                item.ReserveFor(scenario.Order1Id, DateTime.UtcNow);
            }

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            await transaction.RollbackAsync();
        }

        await using var verificationContext = _fixture.CreateContext();
        var allocations = await verificationContext.InventoryItems
            .Where(item => scenario.ItemIds.Contains(item.Id))
            .ToArrayAsync();
        Assert.All(allocations, item =>
        {
            Assert.Equal(InventoryItemStatus.Available, item.Status);
            Assert.Null(item.ReservedForOrderId);
        });
    }

    [SqlServerInventoryAllocationFact]
    public async Task LockingQuery_UsesRequiredHintsIndexAndNoSecretParameters()
    {
        var scenario = await SeedScenarioAsync(
            new ItemSeed(false, DateTime.UtcNow, null));
        var interceptor = new InventoryAllocationCommandInterceptor();
        await using var context = _fixture.CreateContext(interceptor);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var repository = new InventoryCodeFulfillmentRepository(context);

        var selected = await repository.LockNextAvailableAsync(scenario.VariantId);

        Assert.NotNull(selected);
        Assert.Equal(EntityState.Unchanged, context.Entry(selected).State);
        var command = Assert.Single(interceptor.Commands.Where(record =>
            record.CommandText.Contains("UPDLOCK", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains("READPAST", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ROWLOCK", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "IX_InventoryItems_Allocation",
            command.CommandText,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NOLOCK", command.CommandText, StringComparison.OrdinalIgnoreCase);

        var predicate = command.CommandText[command.CommandText.IndexOf(
            "WHERE",
            StringComparison.OrdinalIgnoreCase)..];
        Assert.DoesNotContain("ProtectedCode", predicate, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CodeFingerprint", predicate, StringComparison.OrdinalIgnoreCase);

        var forbiddenPlaintext = string.Concat("plain", "-", "inventory", "-", "secret");
        Assert.All(command.ParameterValues, value =>
            Assert.NotEqual(forbiddenPlaintext, value?.ToString()));
        await transaction.RollbackAsync();
    }

    private async Task<InventoryScenario> SeedScenarioAsync(params ItemSeed[] itemSeeds)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var admin = new User
        {
            FullName = "Inventory test Admin",
            Email = $"inventory-admin-{suffix}@example.test",
            PasswordHash = "test-only-hash",
            Role = UserRole.Admin,
            CreatedAt = now
        };
        var customer1 = CreateCustomer($"customer-1-{suffix}@example.test", now);
        var customer2 = CreateCustomer($"customer-2-{suffix}@example.test", now);
        var category = new ServiceCategory
        {
            Name = $"Inventory {suffix}",
            Slug = $"inventory-{suffix}",
            IsActive = true,
            CreatedAt = now
        };
        var provider = new Provider
        {
            Name = $"Provider {suffix}",
            Slug = $"provider-{suffix}",
            IsActive = true,
            CreatedAt = now
        };
        var service = new DigitalService
        {
            CategoryId = category.Id,
            Category = category,
            ProviderId = provider.Id,
            Provider = provider,
            Name = $"Inventory service {suffix}",
            Slug = $"inventory-service-{suffix}",
            CurrencyCode = "USD",
            BasePrice = 1m,
            IsActive = true,
            CreatedAt = now
        };
        var variant = CreateVariant(service, $"primary-{suffix}", now);
        var otherVariant = CreateVariant(service, $"other-{suffix}", now);
        var order1 = CreateOrder(customer1, service, variant, $"IA-{suffix[..8]}-1", now);
        var order2 = CreateOrder(customer2, service, variant, $"IA-{suffix[..8]}-2", now);

        var items = itemSeeds.Select(seed =>
        {
            var targetVariant = seed.UseOtherVariant ? otherVariant : variant;
            var item = InventoryItem.Create(
                targetVariant.Id,
                FulfillmentMode.InventoryCode,
                $"protected-test-payload::{Guid.NewGuid():N}",
                $"{Guid.NewGuid():N}{Guid.NewGuid():N}",
                admin.Id,
                seed.CreatedAtUtc);
            if (seed.Id.HasValue)
            {
                item.Id = seed.Id.Value;
            }

            return item;
        }).ToArray();

        await using var context = _fixture.CreateContext();
        context.AddRange(
            admin,
            customer1,
            customer2,
            category,
            provider,
            service,
            variant,
            otherVariant,
            order1,
            order2);
        context.InventoryItems.AddRange(items);
        await context.SaveChangesAsync();

        return new InventoryScenario(
            admin.Id,
            variant.Id,
            otherVariant.Id,
            order1.Id,
            order2.Id,
            items.Select(item => item.Id).ToArray());
    }

    private static User CreateCustomer(string email, DateTime createdAtUtc)
    {
        return new User
        {
            FullName = "Inventory test customer",
            Email = email,
            PasswordHash = "test-only-hash",
            Role = UserRole.User,
            CreatedAt = createdAtUtc
        };
    }

    private static ServiceVariant CreateVariant(
        DigitalService service,
        string slug,
        DateTime createdAtUtc)
    {
        return new ServiceVariant
        {
            ServiceId = service.Id,
            Service = service,
            Name = slug,
            Slug = slug,
            CurrencyCode = "USD",
            RegionCode = "GLOBAL",
            FulfillmentMode = FulfillmentMode.InventoryCode,
            BasePrice = 1m,
            IsActive = true,
            CreatedAt = createdAtUtc
        };
    }

    private static Order CreateOrder(
        User customer,
        DigitalService service,
        ServiceVariant variant,
        string orderNumber,
        DateTime createdAtUtc)
    {
        var order = OrderTestFactory.Create(FulfillmentMode.InventoryCode);
        order.OrderNumber = orderNumber;
        order.UserId = customer.Id;
        order.User = customer;
        order.ServiceId = service.Id;
        order.Service = service;
        order.ServiceVariantId = variant.Id;
        order.ServiceVariant = variant;
        order.OrderType = OrderType.Purchase;
        order.FulfillmentMode = OrderFulfillmentMode.UseExistingAccount;
        order.Status = OrderStatus.Paid;
        order.ForeignPrice = 1m;
        order.CurrencyCode = "USD";
        order.ExchangeRateSnapshot = 1m;
        order.MarginPercentSnapshot = 0m;
        order.BaseAmountIrr = 1m;
        order.ProfitAmountIrr = 0m;
        order.FinalAmountIrr = 1m;
        order.PaidAt = createdAtUtc;
        order.CreatedAt = createdAtUtc;
        return order;
    }

    private sealed record ItemSeed(
        bool UseOtherVariant,
        DateTime CreatedAtUtc,
        Guid? Id);

    private sealed record InventoryScenario(
        Guid AdminId,
        Guid VariantId,
        Guid OtherVariantId,
        Guid Order1Id,
        Guid Order2Id,
        IReadOnlyList<Guid> ItemIds);
}
