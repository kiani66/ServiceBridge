using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.ServiceVariantId)
            .IsRequired();

        entity.Property(x => x.ProtectedCode)
            .IsRequired()
            .HasMaxLength(2048);

        entity.Property(x => x.CodeFingerprint)
            .IsRequired()
            .HasMaxLength(64)
            .IsFixedLength();

        entity.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(x => x.CreatedByAdminId)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.RowVersion)
            .IsRowVersion();

        entity.HasIndex(x => x.CodeFingerprint, "UX_InventoryItems_CodeFingerprint")
            .IsUnique();

        entity.HasIndex(
                x => x.ReservedForOrderId,
                "UX_InventoryItems_ReservedForOrderId")
            .IsUnique()
            .HasFilter("[ReservedForOrderId] IS NOT NULL");

        entity.HasIndex(
            x => new { x.ServiceVariantId, x.Status, x.CreatedAt, x.Id },
            "IX_InventoryItems_Allocation");

        entity.HasOne(x => x.ServiceVariant)
            .WithMany()
            .HasForeignKey(x => x.ServiceVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.ReservedForOrder)
            .WithMany()
            .HasForeignKey(x => x.ReservedForOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.DisabledByAdmin)
            .WithMany()
            .HasForeignKey(x => x.DisabledByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_InventoryItems_Status_Valid",
                $"[Status] IN ({(int)InventoryItemStatus.Available}, " +
                $"{(int)InventoryItemStatus.Reserved}, " +
                $"{(int)InventoryItemStatus.Delivered}, " +
                $"{(int)InventoryItemStatus.Disabled})");

            table.HasCheckConstraint(
                "CK_InventoryItems_StateShape",
                $"([Status] = {(int)InventoryItemStatus.Available} " +
                "AND [ReservedForOrderId] IS NULL " +
                "AND [ReservedAtUtc] IS NULL " +
                "AND [DeliveredAtUtc] IS NULL " +
                "AND [DisabledByAdminId] IS NULL " +
                "AND [DisabledAtUtc] IS NULL) OR " +
                $"([Status] = {(int)InventoryItemStatus.Reserved} " +
                "AND [ReservedForOrderId] IS NOT NULL " +
                "AND [ReservedAtUtc] IS NOT NULL " +
                "AND [DeliveredAtUtc] IS NULL " +
                "AND [DisabledByAdminId] IS NULL " +
                "AND [DisabledAtUtc] IS NULL) OR " +
                $"([Status] = {(int)InventoryItemStatus.Delivered} " +
                "AND [ReservedForOrderId] IS NOT NULL " +
                "AND [ReservedAtUtc] IS NOT NULL " +
                "AND [DeliveredAtUtc] IS NOT NULL " +
                "AND [DisabledByAdminId] IS NULL " +
                "AND [DisabledAtUtc] IS NULL) OR " +
                $"([Status] = {(int)InventoryItemStatus.Disabled} " +
                "AND [ReservedForOrderId] IS NULL " +
                "AND [ReservedAtUtc] IS NULL " +
                "AND [DeliveredAtUtc] IS NULL " +
                "AND [DisabledByAdminId] IS NOT NULL " +
                "AND [DisabledAtUtc] IS NOT NULL)");
        });
    }
}
