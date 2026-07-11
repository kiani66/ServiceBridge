using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public sealed class AdminInventoryCommandRepository : IAdminInventoryCommandRepository
{
    private readonly ServiceBridgeDbContext _context;

    public AdminInventoryCommandRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public Task<InventoryServiceVariantTarget?> GetServiceVariantAsync(
        Guid serviceVariantId,
        CancellationToken cancellationToken = default)
    {
        return _context.ServiceVariants
            .AsNoTracking()
            .Where(x => x.Id == serviceVariantId)
            .Select(x => new InventoryServiceVariantTarget
            {
                ServiceVariantId = x.Id,
                ServiceName = x.Service.Name,
                VariantName = x.Name,
                IsActive = x.IsActive,
                FulfillmentMode = x.FulfillmentMode
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> AnyFingerprintsAsync(
        IReadOnlyCollection<string> fingerprints,
        CancellationToken cancellationToken = default)
    {
        return _context.InventoryItems.AnyAsync(
            x => fingerprints.Contains(x.CodeFingerprint),
            cancellationToken);
    }

    public Task StageItemsAsync(
        IReadOnlyCollection<InventoryItem> items,
        CancellationToken cancellationToken = default)
    {
        return _context.InventoryItems.AddRangeAsync(items, cancellationToken);
    }

    public Task<InventoryItem?> GetItemForDisableAsync(
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        return _context.InventoryItems.FirstOrDefaultAsync(
            x => x.Id == inventoryItemId,
            cancellationToken);
    }

    public Task<InventoryWriteResult> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return SaveAsync(cancellationToken);
    }

    private async Task<InventoryWriteResult> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return InventoryWriteResult.Succeeded;
        }
        catch (DbUpdateConcurrencyException)
        {
            return InventoryWriteResult.Conflict;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return InventoryWriteResult.Conflict;
        }
        catch (DbUpdateException)
        {
            return InventoryWriteResult.Failed;
        }
    }
}
