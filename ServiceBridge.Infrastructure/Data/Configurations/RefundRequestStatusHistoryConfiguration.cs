using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public class RefundRequestStatusHistoryConfiguration :
    IEntityTypeConfiguration<RefundRequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RefundRequestStatusHistory> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.NewStatus)
            .IsRequired();

        entity.Property(x => x.ActorType)
            .IsRequired();

        entity.Property(x => x.Note)
            .HasMaxLength(1000);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.HasIndex(x => x.RefundRequestId);

        entity.HasOne(x => x.RefundRequest)
            .WithMany(x => x.StatusHistory)
            .HasForeignKey(x => x.RefundRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_RefundRequestStatusHistory_NewStatus_Valid",
                "[NewStatus] IN (1, 2, 3, 4, 5)");
        });
    }
}
