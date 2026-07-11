using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminInventoryCommandRepository
{
    Task<InventoryServiceVariantTarget?> GetServiceVariantAsync(
        Guid serviceVariantId,
        CancellationToken cancellationToken = default);
    Task<bool> AnyFingerprintsAsync(
        IReadOnlyCollection<string> fingerprints,
        CancellationToken cancellationToken = default);
    Task StageItemsAsync(
        IReadOnlyCollection<InventoryItem> items,
        CancellationToken cancellationToken = default);
    Task<InventoryItem?> GetItemForDisableAsync(
        Guid inventoryItemId,
        CancellationToken cancellationToken = default);
    Task<InventoryWriteResult> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
