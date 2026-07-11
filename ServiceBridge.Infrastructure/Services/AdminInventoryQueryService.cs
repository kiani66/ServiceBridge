using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public sealed class AdminInventoryQueryService : IAdminInventoryQueryService
{
    private const int MaximumPageSize = 100;
    private readonly IAdminInventoryQueryRepository _repository;

    public AdminInventoryQueryService(IAdminInventoryQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<PagedResultDto<AdminInventoryItemDto>>> GetItemsAsync(
        int pageNumber,
        int pageSize,
        Guid? serviceVariantId,
        InventoryItemStatus? status,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > MaximumPageSize)
        {
            return ServiceResult<PagedResultDto<AdminInventoryItemDto>>.Fail(
                AppMessages.InvalidInventoryPage,
                AppConstants.ErrorCodes.ValidationFailed,
                400);
        }

        return ServiceResult<PagedResultDto<AdminInventoryItemDto>>.Ok(
            await _repository.GetItemsAsync(
                pageNumber,
                pageSize,
                serviceVariantId,
                status,
                cancellationToken));
    }

    public async Task<ServiceResult<InventoryAvailabilitySummaryDto>> GetSummaryAsync(
        Guid serviceVariantId,
        CancellationToken cancellationToken = default)
    {
        if (serviceVariantId == Guid.Empty)
        {
            return ServiceResult<InventoryAvailabilitySummaryDto>.Fail(
                AppMessages.ServiceVariantNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var summary = await _repository.GetSummaryAsync(serviceVariantId, cancellationToken);
        return summary is null
            ? ServiceResult<InventoryAvailabilitySummaryDto>.Fail(
                AppMessages.ServiceVariantNotFound,
                AppConstants.ErrorCodes.NotFound,
                404)
            : ServiceResult<InventoryAvailabilitySummaryDto>.Ok(summary);
    }
}
