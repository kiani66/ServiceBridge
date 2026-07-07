using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.AmountIRR)
            .HasPrecision(18, 0)
            .IsRequired();

        entity.Property(x => x.AmountUSD)
            .HasPrecision(18, 4)
            .IsRequired();

        entity.Property(x => x.ExchangeRateSnapshot)
            .HasPrecision(18, 4)
            .IsRequired();

        entity.Property(x => x.Status)
            .IsRequired();

        entity.Property(x => x.GatewayReference)
            .HasMaxLength(200);

        entity.Property(x => x.CallbackPayload)
            .HasColumnType("nvarchar(max)");

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.HasIndex(x => x.OrderId, "IX_Payments_OrderId_Pending")
            .IsUnique()
            .HasFilter($"[Status] = {(int)PaymentStatus.Pending}");

        entity.HasIndex(x => x.OrderId, "IX_Payments_OrderId_Completed")
            .IsUnique()
            .HasFilter($"[Status] = {(int)PaymentStatus.Completed}");

        entity.HasOne(x => x.Order)
            .WithMany(x => x.PaymentRecords)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Navigation(x => x.Events)
            .HasField("_events");

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_Payments_Amounts_Positive",
                "[AmountIRR] > 0 AND [AmountUSD] > 0 AND [ExchangeRateSnapshot] > 0");

            table.HasCheckConstraint(
                "CK_Payments_Status_Valid",
                "[Status] IN (1, 2, 3, 4)");
        });
    }
}
