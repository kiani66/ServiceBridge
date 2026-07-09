using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;
using Xunit;

namespace ServiceBridge.Tests.Data;

public class AuditLogModelTests
{
    private readonly ServiceBridgeDbContext _context;

    public AuditLogModelTests()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ServiceBridgeAuditLogModelTests")
            .Options;

        _context = new ServiceBridgeDbContext(options);
    }

    [Fact]
    public void AuditLog_UsesClientGeneratedId()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog))!;
        var idProperty = entityType.FindProperty(nameof(AuditLog.Id))!;

        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);
    }

    [Fact]
    public void AuditLog_HasExpectedIndexes()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog))!;

        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(x => x.Name)
                .SequenceEqual([nameof(AuditLog.CreatedAtUtc)]));
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(x => x.Name)
                .SequenceEqual([nameof(AuditLog.ActorId)]));
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(x => x.Name)
                .SequenceEqual([nameof(AuditLog.EntityName), nameof(AuditLog.EntityId)]));
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(x => x.Name)
                .SequenceEqual([nameof(AuditLog.ActionType), nameof(AuditLog.CreatedAtUtc)]));
    }

    [Fact]
    public void AuditLog_CannotBeModified()
    {
        var auditLog = new AuditLog
        {
            ActorRole = nameof(UserRole.Admin),
            ActionType = AuditActionType.SettingsChanged,
            EntityName = "Settings",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.AuditLogs.Attach(auditLog);
        auditLog.Description = "Changed after creation";

        var exception = Assert.Throws<InvalidOperationException>(
            () => _context.SaveChanges());

        Assert.Equal("Audit records are append-only.", exception.Message);
    }

    [Fact]
    public void AuditLog_CannotBeDeleted()
    {
        var auditLog = new AuditLog
        {
            ActorRole = nameof(UserRole.Admin),
            ActionType = AuditActionType.SettingsChanged,
            EntityName = "Settings",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.AuditLogs.Attach(auditLog);
        _context.AuditLogs.Remove(auditLog);

        var exception = Assert.Throws<InvalidOperationException>(
            () => _context.SaveChanges());

        Assert.Equal("Audit records are append-only.", exception.Message);
    }
}
