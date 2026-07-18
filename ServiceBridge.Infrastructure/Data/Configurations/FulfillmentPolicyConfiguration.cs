using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public sealed class FulfillmentPolicyConfiguration :
    IEntityTypeConfiguration<FulfillmentPolicy>
{
    public void Configure(EntityTypeBuilder<FulfillmentPolicy> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.ServiceVariantId)
            .IsRequired();

        entity.Property(x => x.Version)
            .IsRequired();

        entity.Property(x => x.ProcurementStrategy)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(x => x.FulfillmentMode)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(x => x.DeliveryArtifactType)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.HasIndex(
                x => new { x.ServiceVariantId, x.Version },
                "UX_FulfillmentPolicies_ServiceVariantId_Version")
            .IsUnique();

        entity.HasOne(x => x.ServiceVariant)
            .WithMany()
            .HasForeignKey(x => x.ServiceVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_FulfillmentPolicies_Version_Positive",
                "[Version] > 0");

            table.HasCheckConstraint(
                "CK_FulfillmentPolicies_ProcurementStrategy_Valid",
                $"[ProcurementStrategy] IN (" +
                $"{(int)ProcurementStrategy.NotRequired}, " +
                $"{(int)ProcurementStrategy.PreStockedInventory}, " +
                $"{(int)ProcurementStrategy.OnDemandManual}, " +
                $"{(int)ProcurementStrategy.OnDemandSupplierApi}, " +
                $"{(int)ProcurementStrategy.PreferInventoryThenManual})");

            table.HasCheckConstraint(
                "CK_FulfillmentPolicies_FulfillmentMode_Valid",
                $"[FulfillmentMode] IN (" +
                $"{(int)FulfillmentMode.ManualAgent}, " +
                $"{(int)FulfillmentMode.InventoryCode}, " +
                $"{(int)FulfillmentMode.ExternalAutomation}, " +
                $"{(int)FulfillmentMode.InstructionOnly})");

            table.HasCheckConstraint(
                "CK_FulfillmentPolicies_DeliveryArtifactType_Valid",
                $"[DeliveryArtifactType] IN (" +
                $"{(int)DeliveryArtifactType.DigitalCode}, " +
                $"{(int)DeliveryArtifactType.GiftLink}, " +
                $"{(int)DeliveryArtifactType.AccountAccess}, " +
                $"{(int)DeliveryArtifactType.Instructions})");
        });
    }
}
