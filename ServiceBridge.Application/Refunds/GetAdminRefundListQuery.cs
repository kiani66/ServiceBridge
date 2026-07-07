using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Refunds;

public class GetAdminRefundListQuery
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 50;

    private readonly IRefundRequestRepository _repository;

    public GetAdminRefundListQuery(IRefundRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<PagedResultDto<AdminRefundRequestDto>>>
        ExecuteAsync(int page, int pageSize, RefundStatus? status)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0
            ? DefaultPageSize
            : Math.Min(pageSize, MaximumPageSize);

        return ServiceResult<PagedResultDto<AdminRefundRequestDto>>.Ok(
            await _repository.GetAdminRefundsAsync(page, pageSize, status));
    }
}
