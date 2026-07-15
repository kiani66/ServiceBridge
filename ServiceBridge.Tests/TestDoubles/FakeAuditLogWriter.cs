using ServiceBridge.Application.Audit;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeAuditLogWriter : IAuditLogWriter
{
    public List<AuditLogEntry> Entries { get; } = new();
    public List<AuditLogEntry> QueuedEntries { get; } = new();
    public bool ThrowOnQueue { get; set; }
    public int? ThrowOnQueueCall { get; set; }
    public int QueueCallCount { get; private set; }
    public IList<string>? Operations { get; set; }

    public Task QueueAsync(
        AuditLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        QueueCallCount++;
        Operations?.Add("audit.queue");
        if (ThrowOnQueue || ThrowOnQueueCall == QueueCallCount)
        {
            throw new InvalidOperationException("Audit queue failed.");
        }

        QueuedEntries.Add(entry);
        return Task.CompletedTask;
    }

    public async Task WriteAsync(
        AuditLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        await QueueAsync(entry, cancellationToken);
        Entries.Add(entry);
    }
}
