using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeInventoryCodeFulfillmentRepository :
    IInventoryCodeFulfillmentRepository
{
    public InventoryItem? ExistingAllocation { get; set; }
    public InventoryItem? NextAvailable { get; set; }
    public Func<bool>? IsTransactionActive { get; set; }
    public IList<string>? Operations { get; set; }
    public int ExistingAllocationCalls { get; private set; }
    public int LockCalls { get; private set; }

    public Task<InventoryItem?> GetExistingAllocationAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        EnsureActiveTransaction();
        ExistingAllocationCalls++;
        Operations?.Add("inventory.existing");
        return Task.FromResult(ExistingAllocation);
    }

    public Task<InventoryItem?> LockNextAvailableAsync(
        Guid serviceVariantId,
        CancellationToken cancellationToken = default)
    {
        EnsureActiveTransaction();
        LockCalls++;
        Operations?.Add("inventory.lock");
        return Task.FromResult(NextAvailable);
    }

    private void EnsureActiveTransaction()
    {
        if (IsTransactionActive is not null && !IsTransactionActive())
        {
            throw new InvalidOperationException(
                "Inventory allocation requires an active transaction.");
        }
    }
}
