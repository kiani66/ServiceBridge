using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class ServiceVariantConfiguration : IEntityTypeConfiguration<ServiceVariant>
{
    public void Configure(EntityTypeBuilder<ServiceVariant> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        entity.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(170);

        entity.Property(x => x.BillingCycle)
            .IsRequired()
            .HasDefaultValue(BillingCycle.None);

        entity.Property(x => x.AccountDeliveryType)
            .IsRequired()
            .HasDefaultValue(AccountDeliveryType.NotApplicable);

        entity.Property(x => x.RegionCode)
            .HasMaxLength(20);

        entity.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        entity.Property(x => x.BasePrice)
            .HasPrecision(18, 4)
            .IsRequired();

        entity.Property(x => x.ProcessingTimeDescription)
            .HasMaxLength(250);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.HasIndex(x => new { x.ServiceId, x.Slug })
            .IsUnique();

        entity.HasIndex(x => new { x.ServiceId, x.IsActive, x.DisplayOrder });

        entity.HasOne(x => x.Service)
            .WithMany(x => x.Variants)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ServiceVariants_BasePrice_NonNegative",
                "[BasePrice] >= 0");

            table.HasCheckConstraint(
                "CK_ServiceVariants_DurationDays_Positive",
                "[DurationDays] IS NULL OR [DurationDays] > 0");

            table.HasCheckConstraint(
                "CK_ServiceVariants_BillingCycle_Valid",
                "[BillingCycle] IN (0, 1, 2, 3, 4, 5, 6)");

            table.HasCheckConstraint(
                "CK_ServiceVariants_AccountDeliveryType_Valid",
                "[AccountDeliveryType] IN (0, 1, 2, 3, 4, 5)");
        });
    }
}
