using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeAdminInventoryQueryRepository : IAdminInventoryQueryRepository
{
    public PagedResultDto<AdminInventoryItemDto> Page { get; set; } = new();
    public InventoryAvailabilitySummaryDto? Summary { get; set; }
    public (int Page, int PageSize, Guid? VariantId, InventoryItemStatus? Status)? LastListRequest { get; private set; }

    public Task<PagedResultDto<AdminInventoryItemDto>> GetItemsAsync(
        int pageNumber,
        int pageSize,
        Guid? serviceVariantId,
        InventoryItemStatus? status,
        CancellationToken cancellationToken = default)
    {
        LastListRequest = (pageNumber, pageSize, serviceVariantId, status);
        return Task.FromResult(Page);
    }

    public Task<InventoryAvailabilitySummaryDto?> GetSummaryAsync(
        Guid serviceVariantId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Summary);
    }
}
