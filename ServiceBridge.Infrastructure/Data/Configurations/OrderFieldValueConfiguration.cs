using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class OrderFieldValueConfiguration : IEntityTypeConfiguration<OrderFieldValue>
{
    public void Configure(EntityTypeBuilder<OrderFieldValue> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.FieldKey)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.FieldLabel)
            .IsRequired()
            .HasMaxLength(150);

        entity.Property(x => x.FulfillmentMode)
            .IsRequired();

        entity.Property(x => x.Actor)
            .IsRequired();

        entity.Property(x => x.EncryptedValue)
            .IsRequired()
            .HasMaxLength(4000);

        entity.Property(x => x.IsSensitive)
            .IsRequired();

        entity.Property(x => x.IsVisibleToCustomerAfterCompletion)
            .IsRequired();

        entity.Property(x => x.DisplayOrder)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired(false);

        entity.HasIndex(x => new
            {
                x.OrderId,
                x.FulfillmentMode,
                x.Actor,
                x.FieldKey
            })
            .IsUnique();

        entity.HasOne(x => x.Order)
            .WithMany(x => x.FieldValues)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
