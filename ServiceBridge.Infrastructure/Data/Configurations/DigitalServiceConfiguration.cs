using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class DigitalServiceConfiguration : IEntityTypeConfiguration<DigitalService>
{
    public void Configure(EntityTypeBuilder<DigitalService> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        entity.Property(x => x.Provider)
            .IsRequired()
            .HasMaxLength(150);

        entity.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(170);

        entity.HasIndex(x => x.Slug)
            .IsUnique();

        entity.Property(x => x.Description)
            .HasMaxLength(2000);

        entity.Property(x => x.LogoUrl)
            .HasMaxLength(500);

        entity.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        entity.Property(x => x.BasePrice)
            .HasPrecision(18, 4)
            .IsRequired();

        entity.Property(x => x.IsActive)
            .IsRequired();

        entity.Property(x => x.SupportsUseExistingAccount)
            .IsRequired();

        entity.Property(x => x.SupportsCreateNewAccount)
            .IsRequired();

        entity.Property(x => x.ProcessingTimeDescription)
            .HasMaxLength(250);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.HasOne(x => x.Category)
            .WithMany(x => x.Services)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
