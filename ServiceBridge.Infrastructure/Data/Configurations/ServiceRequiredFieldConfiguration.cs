using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class ServiceRequiredFieldConfiguration :
    IEntityTypeConfiguration<ServiceRequiredField>
{
    public void Configure(EntityTypeBuilder<ServiceRequiredField> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.FieldKey)
            .IsRequired()
            .HasMaxLength(100);

        entity.HasIndex(x => new
            {
                x.ServiceId,
                x.FulfillmentMode,
                x.Actor,
                x.FieldKey
            })
            .IsUnique();

        entity.Property(x => x.Label)
            .IsRequired()
            .HasMaxLength(150);

        entity.Property(x => x.FieldType)
            .IsRequired();

        entity.Property(x => x.FulfillmentMode)
            .IsRequired();

        entity.Property(x => x.Actor)
            .IsRequired();

        entity.Property(x => x.IsRequired)
            .IsRequired();

        entity.Property(x => x.IsSensitive)
            .IsRequired();

        entity.Property(x => x.IsVisibleToCustomerAfterCompletion)
            .IsRequired();

        entity.Property(x => x.DisplayOrder)
            .IsRequired();

        entity.Property(x => x.OptionsJson)
            .HasMaxLength(2000);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.HasOne(x => x.Service)
            .WithMany(x => x.RequiredFields)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
