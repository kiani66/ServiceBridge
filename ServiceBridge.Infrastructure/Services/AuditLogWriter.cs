using Microsoft.AspNetCore.Http;
using ServiceBridge.Application.Audit;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Services;

public class AuditLogWriter : IAuditLogWriter
{
    private readonly ServiceBridgeDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogWriter(
        ServiceBridgeDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task WriteAsync(
        AuditLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        await QueueAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task QueueAsync(
        AuditLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = entry.ActorId,
            ActorRole = entry.ActorRole.Trim(),
            ActionType = entry.ActionType,
            EntityName = entry.EntityName.Trim(),
            EntityId = entry.EntityId,
            BeforeJson = SensitiveDataRedactor.ToRedactedJson(entry.Before),
            AfterJson = SensitiveDataRedactor.ToRedactedJson(entry.After),
            Description = SensitiveDataRedactor.RedactDescription(entry.Description),
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
            CorrelationId = httpContext?.TraceIdentifier,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }
}
