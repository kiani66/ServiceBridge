using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IInventoryCodeFulfillmentRepository
{
    Task<InventoryItem?> GetExistingAllocationAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<InventoryItem?> LockNextAvailableAsync(
        Guid serviceVariantId,
        CancellationToken cancellationToken = default);
}
