using System.Text.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Inventory;

public class AdminInventoryCommandTests
{
    [Fact]
    public async Task CreateAsync_ActiveInventoryVariant_ImportsOneItemWithSafeAudit()
    {
        var setup = CreateImportSetup();

        var result = await setup.Command.ExecuteAsync(setup.AdminId, Request(setup.Target.ServiceVariantId, "Alpha-01"));

        Assert.True(result.Success);
        Assert.Equal(1, result.Data?.CreatedCount);
        var item = Assert.Single(setup.Repository.PersistedItems);
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(InventoryItemStatus.Available, item.Status);
        Assert.Equal(1, setup.Repository.StageCallCount);
        Assert.Equal(1, setup.Repository.SaveChangesCallCount);
        var audit = Assert.Single(setup.AuditWriter.QueuedEntries);
        Assert.Equal(AuditActionType.InventoryItemCreated, audit.ActionType);
        Assert.DoesNotContain("Alpha-01", JsonSerializer.Serialize(audit));
        Assert.DoesNotContain("ProtectedCode", JsonSerializer.Serialize(audit));
        Assert.DoesNotContain("CodeFingerprint", JsonSerializer.Serialize(audit));
    }

    [Fact]
    public async Task CreateAsync_ImportsValidBatch_Atomically()
    {
        var setup = CreateImportSetup();

        var result = await setup.Command.ExecuteAsync(setup.AdminId, Request(setup.Target.ServiceVariantId, "Alpha", "Beta", "Gamma"));

        Assert.True(result.Success);
        Assert.Equal(3, result.Data?.CreatedCount);
        Assert.Equal(3, setup.Repository.PersistedItems.Count);
        Assert.Equal(3, setup.Repository.StagedItems.Count);
        Assert.Equal(3, setup.AuditWriter.QueuedEntries.Count);
        Assert.Equal(1, setup.Repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_AcceptsExactlyOneHundredItems_WithOneSave()
    {
        var setup = CreateImportSetup();
        var values = Enumerable.Range(1, 100).Select(index => $"Value-{index}").ToArray();

        var result = await setup.Command.ExecuteAsync(
            setup.AdminId,
            Request(setup.Target.ServiceVariantId, values));

        Assert.True(result.Success);
        Assert.Equal(100, setup.Repository.StagedItems.Count);
        Assert.Equal(100, setup.AuditWriter.QueuedEntries.Count);
        Assert.Equal(1, setup.Repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenAuditQueueFails_DoesNotSaveOrReturnSuccess()
    {
        var setup = CreateImportSetup();
        setup.AuditWriter.ThrowOnQueueCall = 2;

        var result = await setup.Command.ExecuteAsync(
            setup.AdminId,
            Request(setup.Target.ServiceVariantId, "Value-1", "Value-2", "Value-3"));

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        Assert.Equal(3, setup.Repository.StagedItems.Count);
        Assert.Empty(setup.Repository.PersistedItems);
        Assert.Single(setup.AuditWriter.QueuedEntries);
        Assert.Equal(2, setup.AuditWriter.QueueCallCount);
        Assert.Equal(0, setup.Repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenPersistenceFails_ReturnsOperationalFailure()
    {
        var setup = CreateImportSetup();
        setup.Repository.SaveResult = InventoryWriteResult.Failed;

        var result = await setup.Command.ExecuteAsync(
            setup.AdminId,
            Request(setup.Target.ServiceVariantId, "Value"));

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        Assert.Single(setup.Repository.StagedItems);
        Assert.Single(setup.AuditWriter.QueuedEntries);
        Assert.Empty(setup.Repository.PersistedItems);
        Assert.Equal(1, setup.Repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_UsesTrimmedValueOnlyForFingerprint()
    {
        var setup = CreateImportSetup();

        await setup.Command.ExecuteAsync(setup.AdminId, Request(setup.Target.ServiceVariantId, "  MiXeD  "));

        Assert.Equal("MiXeD", Assert.Single(setup.Protector.FingerprintInputs));
        Assert.Equal("protected:  MiXeD  ", Assert.Single(setup.Repository.PersistedItems).ProtectedCode);
    }

    [Fact]
    public async Task CreateAsync_InactiveInventoryVariant_ReturnsConflictBeforeProtectionOrStaging()
    {
        var setup = CreateImportSetup(isActive: false);

        var result = await setup.Command.ExecuteAsync(
            setup.AdminId,
            Request(setup.Target.ServiceVariantId, "Value"));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(0, setup.Protector.ProtectCallCount);
        Assert.Empty(setup.Protector.FingerprintInputs);
        Assert.Empty(setup.Repository.StagedItems);
        Assert.Empty(setup.AuditWriter.QueuedEntries);
        Assert.Equal(0, setup.Repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownVariant()
    {
        var setup = CreateImportSetup();
        setup.Repository.Target = null;

        var result = await setup.Command.ExecuteAsync(setup.AdminId, Request(setup.Target.ServiceVariantId, "Alpha"));

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(setup.Repository.PersistedItems);
    }

    [Fact]
    public async Task CreateAsync_RejectsNonInventoryVariant()
    {
        var setup = CreateImportSetup(fulfillmentMode: FulfillmentMode.ManualAgent);

        var result = await setup.Command.ExecuteAsync(setup.AdminId, Request(setup.Target.ServiceVariantId, "Alpha"));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Empty(setup.Repository.PersistedItems);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task CreateAsync_RejectsInvalidBatchSize(int count)
    {
        var setup = CreateImportSetup();
        var request = new CreateInventoryItemsRequest
        {
            ServiceVariantId = setup.Target.ServiceVariantId,
            Codes = Enumerable.Range(0, count).Select(x => $"Value-{x}").ToArray()
        };

        var result = await setup.Command.ExecuteAsync(setup.AdminId, request);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(0, setup.Repository.StageCallCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsWhitespaceOnlyValue()
    {
        var setup = CreateImportSetup();

        var result = await setup.Command.ExecuteAsync(setup.AdminId, Request(setup.Target.ServiceVariantId, "   "));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(0, setup.Repository.StageCallCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateValuesInsideBatch()
    {
        var setup = CreateImportSetup();

        var result = await setup.Command.ExecuteAsync(setup.AdminId, Request(setup.Target.ServiceVariantId, "Alpha", "Alpha"));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(0, setup.Repository.StageCallCount);
        Assert.Empty(setup.AuditWriter.QueuedEntries);
    }

    [Fact]
    public async Task CreateAsync_RejectsValuesDifferingOnlyBySurroundingWhitespace()
    {
        var setup = CreateImportSetup();

        var result = await setup.Command.ExecuteAsync(
            setup.AdminId,
            Request(setup.Target.ServiceVariantId, "  Alpha-01  ", "Alpha-01"));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(0, setup.Protector.ProtectCallCount);
        Assert.Equal(2, setup.Protector.FingerprintInputs.Count);
        Assert.Empty(setup.Repository.StagedItems);
        Assert.Empty(setup.AuditWriter.QueuedEntries);
        Assert.Equal(0, setup.Repository.SaveChangesCallCount);
        Assert.DoesNotContain("Alpha-01", result.Message ?? string.Empty);
    }

    [Fact]
    public async Task CreateAsync_AcceptsCaseDifferentValuesAsDistinct()
    {
        var setup = CreateImportSetup();

        var result = await setup.Command.ExecuteAsync(
            setup.AdminId,
            Request(setup.Target.ServiceVariantId, "Alpha-01", "alpha-01"));

        Assert.True(result.Success);
        Assert.Equal(2, setup.Protector.ProtectCallCount);
        Assert.Equal(2, setup.Protector.FingerprintInputs.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, setup.Repository.StagedItems.Count);
        Assert.Equal(2, setup.AuditWriter.QueuedEntries.Count);
        Assert.Equal(1, setup.Repository.SaveChangesCallCount);
        var auditJson = JsonSerializer.Serialize(setup.AuditWriter.QueuedEntries);
        Assert.DoesNotContain("Alpha-01", auditJson);
        Assert.DoesNotContain("alpha-01", auditJson);
    }

    [Fact]
    public async Task CreateAsync_RejectsStoredFingerprint()
    {
        var setup = CreateImportSetup();
        setup.Repository.StoredFingerprintExists = true;

        var result = await setup.Command.ExecuteAsync(setup.AdminId, Request(setup.Target.ServiceVariantId, "Alpha"));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(0, setup.Repository.StageCallCount);
    }

    [Fact]
    public async Task CreateAsync_MapsUniqueIndexRaceToConflictWithoutPartialImport()
    {
        var setup = CreateImportSetup();
        setup.Repository.SaveResult = InventoryWriteResult.Conflict;

        var result = await setup.Command.ExecuteAsync(setup.AdminId, Request(setup.Target.ServiceVariantId, "Alpha", "Beta"));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Empty(setup.Repository.PersistedItems);
        Assert.Equal(2, setup.Repository.StagedItems.Count);
        Assert.Equal(2, setup.AuditWriter.QueuedEntries.Count);
        Assert.Equal(1, setup.Repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_ReturnsSafeFailure_WhenFingerprintConfigurationIsUnavailable()
    {
        var setup = CreateImportSetup();
        setup.Protector.ThrowOnFingerprint = true;

        var result = await setup.Command.ExecuteAsync(setup.AdminId, Request(setup.Target.ServiceVariantId, "Alpha"));

        Assert.False(result.Success);
        Assert.Equal(503, result.StatusCode);
        Assert.DoesNotContain("Alpha", result.Message ?? string.Empty);
        Assert.Empty(setup.Repository.PersistedItems);
        Assert.Empty(setup.AuditWriter.QueuedEntries);
    }

    [Fact]
    public async Task DisableAsync_DisablesAvailableItemAndWritesSafeAudit()
    {
        var setup = CreateDisableSetup(CreateItem());

        var result = await setup.Command.ExecuteAsync(setup.AdminId, setup.Item.Id);

        Assert.True(result.Success);
        Assert.Equal(InventoryItemStatus.Disabled, setup.Item.Status);
        Assert.Equal(1, setup.Repository.SaveChangesCallCount);
        var audit = Assert.Single(setup.AuditWriter.QueuedEntries);
        Assert.Equal(AuditActionType.InventoryItemDisabled, audit.ActionType);
        Assert.DoesNotContain("Note", JsonSerializer.Serialize(audit));
        Assert.DoesNotContain("ProtectedCode", JsonSerializer.Serialize(audit));
        Assert.DoesNotContain("CodeFingerprint", JsonSerializer.Serialize(audit));
    }

    [Theory]
    [InlineData(InventoryItemStatus.Reserved)]
    [InlineData(InventoryItemStatus.Delivered)]
    [InlineData(InventoryItemStatus.Disabled)]
    public async Task DisableAsync_RejectsNonAvailableItem(InventoryItemStatus status)
    {
        var item = CreateItem();
        if (status == InventoryItemStatus.Reserved)
        {
            item.ReserveFor(Guid.NewGuid(), DateTime.UtcNow);
        }
        else if (status == InventoryItemStatus.Delivered)
        {
            var orderId = Guid.NewGuid();
            item.ReserveFor(orderId, DateTime.UtcNow);
            item.MarkDelivered(orderId, DateTime.UtcNow);
        }
        else
        {
            item.Disable(Guid.NewGuid(), DateTime.UtcNow);
        }

        var setup = CreateDisableSetup(item);
        var result = await setup.Command.ExecuteAsync(setup.AdminId, item.Id);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(0, setup.Repository.SaveChangesCallCount);
        Assert.Empty(setup.AuditWriter.QueuedEntries);
    }

    [Fact]
    public async Task DisableAsync_MapsConcurrencyToConflictWithoutAudit()
    {
        var setup = CreateDisableSetup(CreateItem());
        setup.Repository.SaveResult = InventoryWriteResult.Conflict;

        var result = await setup.Command.ExecuteAsync(setup.AdminId, setup.Item.Id);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(1, setup.Repository.SaveChangesCallCount);
        Assert.Single(setup.AuditWriter.QueuedEntries);
    }

    [Fact]
    public async Task DisableAsync_WhenRepeatedBySameAdmin_ReturnsConflictWithoutAuditOrSave()
    {
        var adminId = Guid.NewGuid();
        var item = CreateItem();
        item.Disable(adminId, DateTime.UtcNow);
        var setup = CreateDisableSetup(item, adminId);

        var result = await setup.Command.ExecuteAsync(adminId, item.Id);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Empty(setup.AuditWriter.QueuedEntries);
        Assert.Equal(0, setup.Repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task DisableAsync_WhenAuditQueueFails_DoesNotSave()
    {
        var setup = CreateDisableSetup(CreateItem());
        setup.AuditWriter.ThrowOnQueue = true;

        var result = await setup.Command.ExecuteAsync(setup.AdminId, setup.Item.Id);

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        Assert.Empty(setup.AuditWriter.QueuedEntries);
        Assert.Equal(0, setup.Repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task DisableAsync_WhenPersistenceFails_ReturnsOperationalFailure()
    {
        var setup = CreateDisableSetup(CreateItem());
        setup.Repository.SaveResult = InventoryWriteResult.Failed;

        var result = await setup.Command.ExecuteAsync(setup.AdminId, setup.Item.Id);

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        Assert.Single(setup.AuditWriter.QueuedEntries);
        Assert.Equal(1, setup.Repository.SaveChangesCallCount);
    }

    private static CreateInventoryItemsRequest Request(Guid serviceVariantId, params string[] values) => new()
    {
        ServiceVariantId = serviceVariantId,
        Codes = values
    };

    private static (CreateInventoryItemsCommand Command, FakeAdminInventoryCommandRepository Repository,
        FakeInventoryCodeProtector Protector, FakeAuditLogWriter AuditWriter, Guid AdminId,
        InventoryServiceVariantTarget Target) CreateImportSetup(
        InventoryServiceVariantTarget? target = null,
        FulfillmentMode fulfillmentMode = FulfillmentMode.InventoryCode,
        bool isActive = true)
    {
        var effectiveTarget = target ?? new InventoryServiceVariantTarget
        {
            ServiceVariantId = Guid.NewGuid(),
            ServiceName = "Service",
            VariantName = "Variant",
            IsActive = isActive,
            FulfillmentMode = fulfillmentMode
        };
        var repository = new FakeAdminInventoryCommandRepository { Target = target ?? effectiveTarget };
        var protector = new FakeInventoryCodeProtector();
        var auditWriter = new FakeAuditLogWriter();
        return (
            new CreateInventoryItemsCommand(repository, protector, auditWriter),
            repository,
            protector,
            auditWriter,
            Guid.NewGuid(),
            effectiveTarget);
    }

    private static (DisableInventoryItemCommand Command, FakeAdminInventoryCommandRepository Repository,
        FakeAuditLogWriter AuditWriter, Guid AdminId, InventoryItem Item) CreateDisableSetup(
        InventoryItem item,
        Guid? adminId = null)
    {
        var repository = new FakeAdminInventoryCommandRepository { Item = item };
        var auditWriter = new FakeAuditLogWriter();
        return (
            new DisableInventoryItemCommand(repository, auditWriter),
            repository,
            auditWriter,
            adminId ?? Guid.NewGuid(),
            item);
    }

    private static InventoryItem CreateItem() => InventoryItem.Create(
        Guid.NewGuid(),
        FulfillmentMode.InventoryCode,
        "protected-value",
        "fingerprint",
        Guid.NewGuid(),
        DateTime.UtcNow);
}
