using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public sealed class DeliveryArtifactConfiguration :
    IEntityTypeConfiguration<DeliveryArtifact>
{
    public void Configure(EntityTypeBuilder<DeliveryArtifact> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.OrderId)
            .IsRequired();

        entity.Property(x => x.FulfillmentExecutionId)
            .IsRequired();

        entity.Property(x => x.FulfillmentAttemptId)
            .IsRequired();

        entity.Property(x => x.ArtifactType)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(x => x.ProtectedValue)
            .HasMaxLength(DeliveryArtifact.ProtectedValueMaxLength)
            .IsRequired();

        entity.Property(x => x.Fingerprint)
            .HasMaxLength(DeliveryArtifact.FingerprintLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();

        entity.Property(x => x.CreatedAtUtc)
            .IsRequired();

        entity.HasIndex(x => x.OrderId, "IX_DeliveryArtifacts_OrderId");
        entity.HasIndex(
            x => x.FulfillmentAttemptId,
            "IX_DeliveryArtifacts_FulfillmentAttemptId");

        entity.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.FulfillmentExecution)
            .WithMany()
            .HasForeignKey(x => new
            {
                x.FulfillmentExecutionId,
                x.OrderId
            })
            .HasPrincipalKey(x => new
            {
                x.Id,
                x.OrderId
            })
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.FulfillmentAttempt)
            .WithMany()
            .HasForeignKey(x => new
            {
                x.FulfillmentAttemptId,
                x.FulfillmentExecutionId
            })
            .HasPrincipalKey(x => new
            {
                x.Id,
                x.FulfillmentExecutionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_DeliveryArtifacts_ArtifactType_Valid",
                $"[ArtifactType] IN ({(int)DeliveryArtifactType.DigitalCode}, " +
                $"{(int)DeliveryArtifactType.GiftLink}, " +
                $"{(int)DeliveryArtifactType.AccountAccess}, " +
                $"{(int)DeliveryArtifactType.Instructions})");

            table.HasCheckConstraint(
                "CK_DeliveryArtifacts_ProtectedValue_Bounded",
                $"LEN([ProtectedValue]) > 0 AND " +
                $"LEN([ProtectedValue]) <= {DeliveryArtifact.ProtectedValueMaxLength}");
        });
    }
}
