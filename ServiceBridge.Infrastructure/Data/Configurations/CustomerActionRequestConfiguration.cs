using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class CustomerActionRequestConfiguration :
    IEntityTypeConfiguration<CustomerActionRequest>
{
    public void Configure(EntityTypeBuilder<CustomerActionRequest> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.RequestText).IsRequired().HasMaxLength(1000);
        entity.Property(x => x.CustomerResponse).HasMaxLength(2000);
        entity.Property(x => x.RequestedAt).IsRequired();
        entity.Property(x => x.IsResolved).IsRequired();
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.Property(x => x.UpdatedAt).IsRequired(false);

        entity.HasIndex(x => new { x.OrderId, x.IsResolved });
        entity.HasIndex(x => new { x.RequestedByAgentId, x.IsResolved });

        entity.HasOne(x => x.Order)
            .WithMany(x => x.CustomerActionRequests)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.RequestedByAgent)
            .WithMany()
            .HasForeignKey(x => x.RequestedByAgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
