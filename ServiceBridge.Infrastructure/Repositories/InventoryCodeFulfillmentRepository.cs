using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public sealed class InventoryCodeFulfillmentRepository :
    IInventoryCodeFulfillmentRepository
{
    private readonly ServiceBridgeDbContext _context;

    public InventoryCodeFulfillmentRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public Task<InventoryItem?> GetExistingAllocationAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        return _context.InventoryItems.SingleOrDefaultAsync(
            item => item.ReservedForOrderId == orderId &&
                    (item.Status == InventoryItemStatus.Reserved ||
                     item.Status == InventoryItemStatus.Delivered),
            cancellationToken);
    }

    public Task<InventoryItem?> LockNextAvailableAsync(
        Guid serviceVariantId,
        CancellationToken cancellationToken = default)
    {
        if (serviceVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Service variant ID is required.",
                nameof(serviceVariantId));
        }

        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "LockNextAvailableAsync requires an active database transaction.");
        }

        return _context.InventoryItems
            .FromSqlInterpolated($$"""
                SELECT TOP (1) [item].*
                FROM [InventoryItems] AS [item] WITH (
                    UPDLOCK,
                    READPAST,
                    ROWLOCK,
                    INDEX([IX_InventoryItems_Allocation])
                )
                WHERE [item].[ServiceVariantId] = {{serviceVariantId}}
                  AND [item].[Status] = {{(int)InventoryItemStatus.Available}}
                ORDER BY [item].[CreatedAt], [item].[Id]
                """)
            .AsTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }
}
