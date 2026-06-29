using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class ProfitMarginConfiguration : IEntityTypeConfiguration<ProfitMargin>
{
    public void Configure(EntityTypeBuilder<ProfitMargin> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.Percentage)
            .HasPrecision(5, 2)
            .IsRequired();

        entity.Property(x => x.IsActive)
            .IsRequired();

        entity.Property(x => x.EffectiveFrom)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.HasIndex(x => new { x.ServiceVariantId, x.IsActive, x.EffectiveFrom });
        entity.HasIndex(x => new { x.ServiceId, x.IsActive, x.EffectiveFrom });
        entity.HasIndex(x => new { x.CategoryId, x.IsActive, x.EffectiveFrom });
        entity.HasIndex(x => new { x.IsActive, x.EffectiveFrom });

        entity.HasOne(x => x.ServiceVariant)
            .WithMany()
            .HasForeignKey(x => x.ServiceVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Category)
            .WithMany(x => x.ProfitMargins)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Service)
            .WithMany(x => x.ProfitMargins)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ProfitMargins_Percentage_Range",
                "[Percentage] >= 0 AND [Percentage] <= 100");

            table.HasCheckConstraint(
                "CK_ProfitMargins_SingleScope",
                "([ServiceVariantId] IS NULL AND [ServiceId] IS NULL AND [CategoryId] IS NULL) OR " +
                "([ServiceVariantId] IS NOT NULL AND [ServiceId] IS NULL AND [CategoryId] IS NULL) OR " +
                "([ServiceVariantId] IS NULL AND [ServiceId] IS NOT NULL AND [CategoryId] IS NULL) OR " +
                "([ServiceVariantId] IS NULL AND [ServiceId] IS NULL AND [CategoryId] IS NOT NULL)");
        });
    }
}
