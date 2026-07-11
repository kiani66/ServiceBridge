using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public sealed class AdminInventoryQueryRepository : IAdminInventoryQueryRepository
{
    private readonly ServiceBridgeDbContext _context;

    public AdminInventoryQueryRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<AdminInventoryItemDto>> GetItemsAsync(
        int pageNumber,
        int pageSize,
        Guid? serviceVariantId,
        InventoryItemStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryItems.AsNoTracking();

        if (serviceVariantId.HasValue)
        {
            query = query.Where(x => x.ServiceVariantId == serviceVariantId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminInventoryItemDto
            {
                InventoryItemId = x.Id,
                ServiceVariantId = x.ServiceVariantId,
                ServiceName = x.ServiceVariant.Service.Name,
                VariantName = x.ServiceVariant.Name,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                CreatedByAdminName = x.CreatedByAdmin.FullName,
                ReservedForOrderId = x.ReservedForOrderId,
                ReservedAtUtc = x.ReservedAtUtc,
                DeliveredAtUtc = x.DeliveredAtUtc,
                DisabledAtUtc = x.DisabledAtUtc,
                DisabledByAdminName = x.DisabledByAdmin == null
                    ? null
                    : x.DisabledByAdmin.FullName
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<AdminInventoryItemDto>
        {
            Items = items,
            Page = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<InventoryAvailabilitySummaryDto?> GetSummaryAsync(
        Guid serviceVariantId,
        CancellationToken cancellationToken = default)
    {
        var variant = await _context.ServiceVariants
            .AsNoTracking()
            .Where(x => x.Id == serviceVariantId)
            .Select(x => new
            {
                x.Id,
                ServiceName = x.Service.Name,
                VariantName = x.Name
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (variant is null)
        {
            return null;
        }

        var counts = await _context.InventoryItems
            .AsNoTracking()
            .Where(x => x.ServiceVariantId == serviceVariantId)
            .GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        return new InventoryAvailabilitySummaryDto
        {
            ServiceVariantId = variant.Id,
            ServiceName = variant.ServiceName,
            VariantName = variant.VariantName,
            Available = GetCount(InventoryItemStatus.Available),
            Reserved = GetCount(InventoryItemStatus.Reserved),
            Delivered = GetCount(InventoryItemStatus.Delivered),
            Disabled = GetCount(InventoryItemStatus.Disabled),
            Total = counts.Values.Sum()
        };

        int GetCount(InventoryItemStatus status) =>
            counts.TryGetValue(status, out var count) ? count : 0;
    }
}
