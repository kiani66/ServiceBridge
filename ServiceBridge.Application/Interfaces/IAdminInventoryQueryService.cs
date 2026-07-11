using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminInventoryQueryService
{
    public const int DefaultPageSize = 25;
    public const int DefaultVariantOptionsPageSize = 100;

    Task<ServiceResult<PagedResultDto<AdminInventoryItemDto>>> GetItemsAsync(
        int pageNumber,
        int pageSize,
        Guid? serviceVariantId,
        InventoryItemStatus? status,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<InventoryAvailabilitySummaryDto>> GetSummaryAsync(
        Guid serviceVariantId,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<IReadOnlyList<AdminInventoryVariantOptionDto>>> GetVariantOptionsAsync(
        string? search,
        int pageSize,
        CancellationToken cancellationToken = default);
}
