using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Refunds;

public class GetUserRefundHistoryQuery
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 50;

    private readonly IRefundRequestRepository _repository;

    public GetUserRefundHistoryQuery(IRefundRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<PagedResultDto<RefundRequestDto>>> ExecuteAsync(
        Guid userId,
        int page,
        int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0
            ? DefaultPageSize
            : Math.Min(pageSize, MaximumPageSize);

        return ServiceResult<PagedResultDto<RefundRequestDto>>.Ok(
            await _repository.GetUserRefundsAsync(userId, page, pageSize));
    }
}
