using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.ActorRole).IsRequired().HasMaxLength(100);
        entity.Property(x => x.ActionType).IsRequired();
        entity.Property(x => x.EntityName).IsRequired().HasMaxLength(200);
        entity.Property(x => x.BeforeJson).HasColumnType("nvarchar(max)");
        entity.Property(x => x.AfterJson).HasColumnType("nvarchar(max)");
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.Property(x => x.IpAddress).HasMaxLength(100);
        entity.Property(x => x.UserAgent).HasMaxLength(512);
        entity.Property(x => x.CorrelationId).HasMaxLength(100);
        entity.Property(x => x.CreatedAtUtc).IsRequired();

        entity.HasIndex(x => x.CreatedAtUtc);
        entity.HasIndex(x => x.ActorId);
        entity.HasIndex(x => new { x.EntityName, x.EntityId });
        entity.HasIndex(x => new { x.ActionType, x.CreatedAtUtc });
    }
}
