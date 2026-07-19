using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.OrderNumber)
            .IsRequired()
            .HasMaxLength(30);

        entity.HasIndex(x => x.OrderNumber)
            .IsUnique();

        entity.Property(x => x.OrderType)
            .IsRequired();

        entity.Property(x => x.FulfillmentMode)
            .IsRequired();

        entity.Property(x => x.ServiceFulfillmentModeSnapshot)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(x => x.FulfillmentPolicyVersionSnapshot)
            .IsRequired(false);

        entity.Property(x => x.ProcurementStrategySnapshot)
            .HasConversion<int?>()
            .IsRequired(false);

        entity.Property(x => x.PolicyFulfillmentModeSnapshot)
            .HasConversion<int?>()
            .IsRequired(false);

        entity.Property(x => x.DeliveryArtifactTypeSnapshot)
            .HasConversion<int?>()
            .IsRequired(false);

        entity.Property(x => x.Status)
            .IsRequired();

        entity.Property(x => x.ForeignPrice)
            .HasPrecision(18, 4)
            .IsRequired();

        entity.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        entity.Property(x => x.ExchangeRateSnapshot)
            .HasPrecision(18, 4)
            .IsRequired();

        entity.Property(x => x.MarginPercentSnapshot)
            .HasPrecision(5, 2)
            .IsRequired();

        entity.Property(x => x.BaseAmountIrr)
            .HasPrecision(18, 0)
            .IsRequired();

        entity.Property(x => x.ProfitAmountIrr)
            .HasPrecision(18, 0)
            .IsRequired();

        entity.Property(x => x.FinalAmountIrr)
            .HasPrecision(18, 0)
            .IsRequired();

        entity.Property(x => x.VariantNameSnapshot)
            .HasMaxLength(150);

        entity.Property(x => x.VariantRegionCodeSnapshot)
            .HasMaxLength(20);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.Property(x => x.RowVersion)
            .IsRowVersion();

        entity.HasIndex(x => new { x.UserId, x.CreatedAt });
        entity.HasIndex(x => new { x.AssignedAgentId, x.Status });
        entity.HasIndex(x => new { x.Status, x.CreatedAt });

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Service)
            .WithMany()
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.ServiceVariant)
            .WithMany()
            .HasForeignKey(x => x.ServiceVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.FulfillmentPolicy)
            .WithMany()
            .HasForeignKey(x => x.FulfillmentPolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.AssignedAgent)
            .WithMany()
            .HasForeignKey(x => x.AssignedAgentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Navigation(x => x.StatusHistory)
            .HasField("_statusHistory");
        entity.Navigation(x => x.Assignments)
            .HasField("_assignments");
        entity.Navigation(x => x.CustomerActionRequests)
            .HasField("_customerActionRequests");

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_Orders_Pricing_NonNegative",
                "[ForeignPrice] >= 0 AND [ExchangeRateSnapshot] > 0 " +
                "AND [MarginPercentSnapshot] >= 0 AND [MarginPercentSnapshot] <= 100 " +
                "AND [BaseAmountIrr] >= 0 AND [ProfitAmountIrr] >= 0 AND [FinalAmountIrr] >= 0");

            table.HasCheckConstraint(
                "CK_Orders_FinalAmount_Matches",
                "[FinalAmountIrr] = [BaseAmountIrr] + [ProfitAmountIrr]");

            table.HasCheckConstraint(
                "CK_Orders_ServiceFulfillmentModeSnapshot_Valid",
                "[ServiceFulfillmentModeSnapshot] IN (1, 2, 3, 4)");

            table.HasCheckConstraint(
                "CK_Orders_FulfillmentPolicySnapshot_Complete",
                "([FulfillmentPolicyId] IS NULL " +
                "AND [FulfillmentPolicyVersionSnapshot] IS NULL " +
                "AND [ProcurementStrategySnapshot] IS NULL " +
                "AND [PolicyFulfillmentModeSnapshot] IS NULL " +
                "AND [DeliveryArtifactTypeSnapshot] IS NULL) OR " +
                "([FulfillmentPolicyId] IS NOT NULL " +
                "AND [FulfillmentPolicyVersionSnapshot] IS NOT NULL " +
                "AND [FulfillmentPolicyVersionSnapshot] > 0 " +
                "AND [ProcurementStrategySnapshot] IS NOT NULL " +
                $"AND [ProcurementStrategySnapshot] IN ({(int)ProcurementStrategy.NotRequired}, " +
                $"{(int)ProcurementStrategy.PreStockedInventory}, " +
                $"{(int)ProcurementStrategy.OnDemandManual}, " +
                $"{(int)ProcurementStrategy.OnDemandSupplierApi}, " +
                $"{(int)ProcurementStrategy.PreferInventoryThenManual}) " +
                "AND [PolicyFulfillmentModeSnapshot] IS NOT NULL " +
                $"AND [PolicyFulfillmentModeSnapshot] IN ({(int)FulfillmentMode.ManualAgent}, " +
                $"{(int)FulfillmentMode.InventoryCode}, " +
                $"{(int)FulfillmentMode.ExternalAutomation}, " +
                $"{(int)FulfillmentMode.InstructionOnly}) " +
                "AND [DeliveryArtifactTypeSnapshot] IS NOT NULL " +
                $"AND [DeliveryArtifactTypeSnapshot] IN ({(int)DeliveryArtifactType.DigitalCode}, " +
                $"{(int)DeliveryArtifactType.GiftLink}, " +
                $"{(int)DeliveryArtifactType.AccountAccess}, " +
                $"{(int)DeliveryArtifactType.Instructions}))");
        });
    }
}
