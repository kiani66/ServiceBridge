using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class PaymentEventConfiguration :
    IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.EventType).IsRequired();
        entity.Property(x => x.GatewayReference).HasMaxLength(200);
        entity.Property(x => x.Payload).HasColumnType("nvarchar(max)");
        entity.Property(x => x.Note).HasMaxLength(1000);
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.Property(x => x.UpdatedAt).IsRequired(false);

        entity.HasIndex(x => new { x.PaymentId, x.CreatedAt });

        entity.HasOne(x => x.Payment)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
