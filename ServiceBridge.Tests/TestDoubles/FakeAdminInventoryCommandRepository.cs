using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeAdminInventoryCommandRepository : IAdminInventoryCommandRepository
{
    public InventoryServiceVariantTarget? Target { get; set; }
    public InventoryItem? Item { get; set; }
    public bool StoredFingerprintExists { get; set; }
    public InventoryWriteResult SaveResult { get; set; } = InventoryWriteResult.Succeeded;
    public List<InventoryItem> StagedItems { get; } = new();
    public List<InventoryItem> PersistedItems { get; } = new();
    public int StageCallCount { get; private set; }
    public int SaveChangesCallCount { get; private set; }

    public Task<InventoryServiceVariantTarget?> GetServiceVariantAsync(
        Guid serviceVariantId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Target?.ServiceVariantId == serviceVariantId ? Target : null);
    }

    public Task<bool> AnyFingerprintsAsync(
        IReadOnlyCollection<string> fingerprints,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(StoredFingerprintExists);
    }

    public Task StageItemsAsync(
        IReadOnlyCollection<InventoryItem> items,
        CancellationToken cancellationToken = default)
    {
        StageCallCount++;
        StagedItems.AddRange(items);
        return Task.CompletedTask;
    }

    public Task<InventoryItem?> GetItemForDisableAsync(
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Item?.Id == inventoryItemId ? Item : null);
    }

    public Task<InventoryWriteResult> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        if (SaveResult == InventoryWriteResult.Succeeded)
        {
            PersistedItems.AddRange(StagedItems);
        }

        return Task.FromResult(SaveResult);
    }
}
