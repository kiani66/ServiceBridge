using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class MessageOutboxConfiguration : IEntityTypeConfiguration<MessageOutbox>
{
    public void Configure(EntityTypeBuilder<MessageOutbox> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.Channel)
            .IsRequired();

        entity.Property(x => x.Recipient)
            .IsRequired()
            .HasMaxLength(320);

        entity.Property(x => x.Subject)
            .HasMaxLength(200);

        entity.Property(x => x.Body)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        entity.Property(x => x.Status)
            .IsRequired();

        entity.Property(x => x.RelatedEntityType)
            .HasMaxLength(80);

        entity.Property(x => x.Error)
            .HasMaxLength(1000);

        entity.Property(x => x.AttemptCount)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.Property(x => x.NextAttemptAt)
            .IsRequired(false);

        entity.Property(x => x.ProcessedAt)
            .IsRequired(false);

        entity.HasIndex(x => new { x.Status, x.NextAttemptAt, x.CreatedAt });
        entity.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId });
        entity.HasIndex(x => new { x.UserId, x.CreatedAt });

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_MessageOutbox_Channel_Valid",
                $"[Channel] IN ({(int)MessageChannel.Email}, {(int)MessageChannel.Sms})");

            table.HasCheckConstraint(
                "CK_MessageOutbox_Status_Valid",
                $"[Status] IN ({(int)MessageStatus.Pending}, {(int)MessageStatus.Processing}, {(int)MessageStatus.Sent}, {(int)MessageStatus.Failed}, {(int)MessageStatus.Cancelled})");

            table.HasCheckConstraint(
                "CK_MessageOutbox_AttemptCount_NonNegative",
                "[AttemptCount] >= 0");
        });
    }
}
