using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminInventoryQueryRepository
{
    Task<PagedResultDto<AdminInventoryItemDto>> GetItemsAsync(
        int pageNumber,
        int pageSize,
        Guid? serviceVariantId,
        InventoryItemStatus? status,
        CancellationToken cancellationToken = default);
    Task<InventoryAvailabilitySummaryDto?> GetSummaryAsync(
        Guid serviceVariantId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminInventoryVariantOptionDto>> GetVariantOptionsAsync(
        string? search,
        int pageSize,
        CancellationToken cancellationToken = default);
}
