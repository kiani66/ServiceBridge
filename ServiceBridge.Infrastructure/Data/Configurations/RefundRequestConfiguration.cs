using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequest>
{
    public void Configure(EntityTypeBuilder<RefundRequest> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.Amount)
            .HasPrecision(18, 0)
            .IsRequired();

        entity.Property(x => x.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        entity.Property(x => x.AdminNote)
            .HasMaxLength(1000);

        entity.Property(x => x.Status)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.HasIndex(x => new { x.OrderId, x.PaymentId, x.Status });
        entity.HasIndex(x => x.RequestedByUserId);
        entity.HasIndex(x => x.ReviewedByAdminId);

        entity.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Navigation(x => x.StatusHistory)
            .HasField("_statusHistory");

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_RefundRequests_Amount_Positive",
                "[Amount] > 0");

            table.HasCheckConstraint(
                "CK_RefundRequests_Status_Valid",
                "[Status] IN (1, 2, 3, 4, 5)");
        });
    }
}
