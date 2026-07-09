using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IAuditLogQueryRepository
{
    Task<PagedResultDto<AuditLogListItemDto>> GetAuditLogsAsync(
        int page,
        int pageSize,
        Guid? actorId,
        string? entityName,
        Guid? entityId,
        AuditActionType? actionType,
        CancellationToken cancellationToken = default);
}
