using ServiceBridge.Application.Audit;

namespace ServiceBridge.Application.Interfaces;

public interface IAuditLogWriter
{
    Task WriteAsync(
        AuditLogEntry entry,
        CancellationToken cancellationToken = default);
}
