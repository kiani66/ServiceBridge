using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(150);

        entity.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        entity.HasIndex(x => x.Email)
            .IsUnique();

        entity.Property(x => x.PasswordHash)
            .IsRequired();

        entity.Property(x => x.Role)
            .IsRequired();

        entity.Property(x => x.IsActive)
            .IsRequired();

        entity.Property(x => x.ProfileImageUrl)
            .HasMaxLength(500);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);
    }
}
