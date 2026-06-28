using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class AgentAssignmentConfiguration :
    IEntityTypeConfiguration<AgentAssignment>
{
    public void Configure(EntityTypeBuilder<AgentAssignment> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.AssignedAt).IsRequired();
        entity.Property(x => x.Notes).HasMaxLength(1000);
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.Property(x => x.UpdatedAt).IsRequired(false);

        entity.HasIndex(x => new { x.OrderId, x.AssignedAt });

        entity.HasOne(x => x.Order)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.Agent)
            .WithMany()
            .HasForeignKey(x => x.AgentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.AssignedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.AssignedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
