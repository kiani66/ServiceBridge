using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(160);

        entity.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(500);

        entity.Property(x => x.Type)
            .IsRequired();

        entity.Property(x => x.RelatedEntityType)
            .IsRequired()
            .HasMaxLength(80);

        entity.Property(x => x.IsRead)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.Property(x => x.ReadAt)
            .IsRequired(false);

        entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        entity.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId });

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
