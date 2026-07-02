using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.TicketNumber)
            .IsRequired()
            .HasMaxLength(30);

        entity.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.Status)
            .IsRequired();

        entity.Property(x => x.Priority)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.Property(x => x.LastActivityAt)
            .IsRequired();

        entity.Property(x => x.FirstResponseAt)
            .IsRequired(false);

        entity.Property(x => x.ResolvedAt)
            .IsRequired(false);

        entity.Property(x => x.ClosedAt)
            .IsRequired(false);

        entity.Property(x => x.RowVersion)
            .IsRowVersion();

        entity.HasIndex(x => x.TicketNumber)
            .IsUnique();

        entity.HasIndex(x => x.UserId);
        entity.HasIndex(x => x.OrderId);
        entity.HasIndex(x => x.Status);
        entity.HasIndex(x => x.Priority);
        entity.HasIndex(x => x.AssignedAgentId);
        entity.HasIndex(x => x.CreatedAt);
        entity.HasIndex(x => x.LastActivityAt);

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(x => x.AssignedAgent)
            .WithMany()
            .HasForeignKey(x => x.AssignedAgentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Navigation(x => x.Messages)
            .HasField("_messages");
    }
}
