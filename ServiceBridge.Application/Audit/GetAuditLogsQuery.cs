using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Audit;

public class GetAuditLogsQuery
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    private readonly IAuditLogQueryRepository _repository;

    public GetAuditLogsQuery(IAuditLogQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<PagedResultDto<AuditLogListItemDto>>>
        ExecuteAsync(
            int page,
            int pageSize,
            Guid? actorId,
            string? entityName,
            Guid? entityId,
            AuditActionType? actionType,
            CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = NormalizePageSize(pageSize);

        var logs = await _repository.GetAuditLogsAsync(
            page,
            pageSize,
            actorId,
            entityName,
            entityId,
            actionType,
            cancellationToken);

        return ServiceResult<PagedResultDto<AuditLogListItemDto>>.Ok(logs);
    }

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize <= 0
            ? DefaultPageSize
            : Math.Min(pageSize, MaximumPageSize);
    }
}
