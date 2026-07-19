using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public sealed class FulfillmentExecutionConfiguration :
    IEntityTypeConfiguration<FulfillmentExecution>
{
    public void Configure(EntityTypeBuilder<FulfillmentExecution> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.OrderId)
            .IsRequired();

        entity.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(x => x.LastAttemptNumber)
            .IsRequired();

        entity.Property(x => x.StartedAtUtc)
            .IsRequired(false);

        entity.Property(x => x.CompletedAtUtc)
            .IsRequired(false);

        entity.Property(x => x.RowVersion)
            .IsRowVersion();

        entity.HasIndex(x => x.OrderId, "UX_FulfillmentExecutions_OrderId")
            .IsUnique();

        entity.HasOne(x => x.Order)
            .WithOne()
            .HasForeignKey<FulfillmentExecution>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Navigation(x => x.Attempts)
            .HasField("_attempts");

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_FulfillmentExecutions_Status_Valid",
                $"[Status] IN ({(int)FulfillmentExecutionStatus.Pending}, " +
                $"{(int)FulfillmentExecutionStatus.Processing}, " +
                $"{(int)FulfillmentExecutionStatus.Completed}, " +
                $"{(int)FulfillmentExecutionStatus.Failed})");

            table.HasCheckConstraint(
                "CK_FulfillmentExecutions_LastAttemptNumber_NonNegative",
                "[LastAttemptNumber] >= 0");

            table.HasCheckConstraint(
                "CK_FulfillmentExecutions_StateShape",
                $"([Status] = {(int)FulfillmentExecutionStatus.Pending} " +
                "AND [CompletedAtUtc] IS NULL " +
                "AND (([LastAttemptNumber] = 0 AND [StartedAtUtc] IS NULL) " +
                "OR ([LastAttemptNumber] > 0 AND [StartedAtUtc] IS NOT NULL))) OR " +
                $"([Status] = {(int)FulfillmentExecutionStatus.Processing} " +
                "AND [LastAttemptNumber] > 0 " +
                "AND [StartedAtUtc] IS NOT NULL " +
                "AND [CompletedAtUtc] IS NULL) OR " +
                $"([Status] IN ({(int)FulfillmentExecutionStatus.Completed}, " +
                $"{(int)FulfillmentExecutionStatus.Failed}) " +
                "AND [LastAttemptNumber] > 0 " +
                "AND [StartedAtUtc] IS NOT NULL " +
                "AND [CompletedAtUtc] IS NOT NULL " +
                "AND [CompletedAtUtc] >= [StartedAtUtc])");
        });
    }
}
