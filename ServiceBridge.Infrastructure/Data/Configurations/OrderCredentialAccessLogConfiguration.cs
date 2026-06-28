using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class OrderCredentialAccessLogConfiguration :
    IEntityTypeConfiguration<OrderCredentialAccessLog>
{
    public void Configure(EntityTypeBuilder<OrderCredentialAccessLog> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.AccessType).IsRequired().HasMaxLength(100);
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.Property(x => x.UpdatedAt).IsRequired(false);

        entity.HasIndex(x => new { x.OrderId, x.CreatedAt });
        entity.HasIndex(x => new { x.AccessedByUserId, x.CreatedAt });

        entity.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.AccessedByUser)
            .WithMany()
            .HasForeignKey(x => x.AccessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
