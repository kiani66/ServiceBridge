using ServiceBridge.Application.Audit;

namespace ServiceBridge.Application.Interfaces;

public interface IAuditLogWriter
{
    Task QueueAsync(
        AuditLogEntry entry,
        CancellationToken cancellationToken = default);

    Task WriteAsync(
        AuditLogEntry entry,
        CancellationToken cancellationToken = default);
}
