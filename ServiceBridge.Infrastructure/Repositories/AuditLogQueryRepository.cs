using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class AuditLogQueryRepository : IAuditLogQueryRepository
{
    private readonly ServiceBridgeDbContext _context;

    public AuditLogQueryRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<AuditLogListItemDto>> GetAuditLogsAsync(
        int page,
        int pageSize,
        Guid? actorId,
        string? entityName,
        Guid? entityId,
        AuditActionType? actionType,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (actorId.HasValue)
        {
            query = query.Where(x => x.ActorId == actorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            var normalizedEntityName = entityName.Trim();
            query = query.Where(x => x.EntityName == normalizedEntityName);
        }

        if (entityId.HasValue)
        {
            query = query.Where(x => x.EntityId == entityId.Value);
        }

        if (actionType.HasValue)
        {
            query = query.Where(x => x.ActionType == actionType.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogListItemDto
            {
                Id = x.Id,
                CreatedAtUtc = x.CreatedAtUtc,
                ActorId = x.ActorId,
                ActorRole = x.ActorRole,
                ActionType = x.ActionType.ToString(),
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Description = x.Description
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<AuditLogListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
