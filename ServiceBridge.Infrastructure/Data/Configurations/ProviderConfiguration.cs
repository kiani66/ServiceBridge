using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> entity)
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

        entity.HasIndex(x => x.Slug)
            .IsUnique();

        entity.Property(x => x.WebsiteUrl)
            .HasMaxLength(500);

        entity.Property(x => x.LogoUrl)
            .HasMaxLength(500);

        entity.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.HasMany(x => x.Services)
            .WithOne(x => x.Provider)
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
