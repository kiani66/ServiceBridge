using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.Audit;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Audit;

public class AuditLogWriterTests
{
    [Fact]
    public async Task WriteAsync_CreatesAuditRowWithActorActionEntityAndTimestamp()
    {
        var context = new CapturingAuditDbContext();
        var writer = new AuditLogWriter(context, CreateHttpContextAccessor());
        var actorId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await writer.WriteAsync(new AuditLogEntry
        {
            ActorId = actorId,
            ActorRole = nameof(UserRole.Admin),
            ActionType = AuditActionType.OrderAssigned,
            EntityName = nameof(Order),
            EntityId = orderId,
            Description = "Order assigned."
        });

        var row = Assert.Single(context.SavedAuditLogs);
        Assert.NotEqual(Guid.Empty, row.Id);
        Assert.Equal(actorId, row.ActorId);
        Assert.Equal(nameof(UserRole.Admin), row.ActorRole);
        Assert.Equal(AuditActionType.OrderAssigned, row.ActionType);
        Assert.Equal(nameof(Order), row.EntityName);
        Assert.Equal(orderId, row.EntityId);
        Assert.Equal("Order assigned.", row.Description);
        Assert.True(DateTime.UtcNow.Subtract(row.CreatedAtUtc) < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task WriteAsync_RedactsSensitiveValuesBeforePersistence()
    {
        var context = new CapturingAuditDbContext();
        var writer = new AuditLogWriter(context, CreateHttpContextAccessor());

        await writer.WriteAsync(new AuditLogEntry
        {
            ActorId = Guid.NewGuid(),
            ActorRole = nameof(UserRole.Admin),
            ActionType = AuditActionType.SettingsChanged,
            EntityName = "Settings",
            EntityId = Guid.NewGuid(),
            Before = new { Password = "old-password", Name = "Public" },
            After = new { Token = "token-value", Name = "Public" },
            Description = "Settings changed."
        });

        var row = Assert.Single(context.SavedAuditLogs);
        Assert.Contains("[REDACTED]", row.BeforeJson);
        Assert.Contains("[REDACTED]", row.AfterJson);
        Assert.Contains("Public", row.BeforeJson);
        Assert.Contains("Public", row.AfterJson);
        Assert.DoesNotContain("old-password", row.BeforeJson);
        Assert.DoesNotContain("token-value", row.AfterJson);
    }

    [Fact]
    public async Task WriteAsync_CredentialViewedDoesNotStoreCredentialValues()
    {
        var context = new CapturingAuditDbContext();
        var writer = new AuditLogWriter(context, CreateHttpContextAccessor());

        await writer.WriteAsync(new AuditLogEntry
        {
            ActorId = Guid.NewGuid(),
            ActorRole = nameof(UserRole.Agent),
            ActionType = AuditActionType.CredentialViewed,
            EntityName = nameof(Order),
            EntityId = Guid.NewGuid(),
            Description = "Agent viewed protected order fields for order SB-1001."
        });

        var row = Assert.Single(context.SavedAuditLogs);
        Assert.Null(row.BeforeJson);
        Assert.Null(row.AfterJson);
        Assert.DoesNotContain("password", row.Description!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", row.Description!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", row.Description!, StringComparison.OrdinalIgnoreCase);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor()
    {
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = "trace-1"
            }
        };
    }

    private sealed class CapturingAuditDbContext : ServiceBridgeDbContext
    {
        public CapturingAuditDbContext()
            : base(new DbContextOptionsBuilder<ServiceBridgeDbContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ServiceBridgeAuditWriterTests")
                .Options)
        {
        }

        public List<AuditLog> SavedAuditLogs { get; } = new();

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            SavedAuditLogs.AddRange(ChangeTracker
                .Entries<AuditLog>()
                .Where(x => x.State == EntityState.Added)
                .Select(x => x.Entity));

            return Task.FromResult(SavedAuditLogs.Count);
        }
    }
}
