using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.ValueObjects;

namespace ServiceBridge.Infrastructure.Data.Configurations;

public sealed class FulfillmentAttemptConfiguration :
    IEntityTypeConfiguration<FulfillmentAttempt>
{
    public void Configure(EntityTypeBuilder<FulfillmentAttempt> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .ValueGeneratedNever();

        entity.Property(x => x.FulfillmentExecutionId)
            .IsRequired();

        entity.Property(x => x.AttemptNumber)
            .IsRequired();

        entity.Property(x => x.FulfillmentMode)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(x => x.StartedAtUtc)
            .IsRequired();

        entity.Property(x => x.CompletedAtUtc)
            .IsRequired(false);

        entity.Property(x => x.FailureDisposition)
            .HasConversion<int?>()
            .IsRequired(false);

        entity.Property(x => x.FailureCode)
            .HasConversion(
                code => code!.Value,
                value => FulfillmentFailureCode.Create(value))
            .HasMaxLength(FulfillmentFailureCode.MaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        entity.Property(x => x.FailureMessage)
            .HasMaxLength(FulfillmentAttempt.FailureMessageMaxLength)
            .IsRequired(false);

        entity.HasIndex(
                x => new { x.FulfillmentExecutionId, x.AttemptNumber },
                "UX_FulfillmentAttempts_ExecutionId_AttemptNumber")
            .IsUnique();

        entity.HasOne<FulfillmentExecution>()
            .WithMany(x => x.Attempts)
            .HasForeignKey(x => x.FulfillmentExecutionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_FulfillmentAttempts_AttemptNumber_Positive",
                "[AttemptNumber] > 0");

            table.HasCheckConstraint(
                "CK_FulfillmentAttempts_FulfillmentMode_Valid",
                $"[FulfillmentMode] IN ({(int)FulfillmentMode.ManualAgent}, " +
                $"{(int)FulfillmentMode.InventoryCode}, " +
                $"{(int)FulfillmentMode.ExternalAutomation}, " +
                $"{(int)FulfillmentMode.InstructionOnly})");

            table.HasCheckConstraint(
                "CK_FulfillmentAttempts_Status_Valid",
                $"[Status] IN ({(int)FulfillmentAttemptStatus.Processing}, " +
                $"{(int)FulfillmentAttemptStatus.Succeeded}, " +
                $"{(int)FulfillmentAttemptStatus.Failed})");

            table.HasCheckConstraint(
                "CK_FulfillmentAttempts_FailureDisposition_Valid",
                $"[FailureDisposition] IS NULL OR [FailureDisposition] IN (" +
                $"{(int)FulfillmentFailureDisposition.Retryable}, " +
                $"{(int)FulfillmentFailureDisposition.Terminal})");

            table.HasCheckConstraint(
                "CK_FulfillmentAttempts_CompletionChronology",
                "[CompletedAtUtc] IS NULL OR [CompletedAtUtc] >= [StartedAtUtc]");

            table.HasCheckConstraint(
                "CK_FulfillmentAttempts_FailureCode_NonEmpty",
                "[FailureCode] IS NULL OR LEN([FailureCode]) > 0");

            table.HasCheckConstraint(
                "CK_FulfillmentAttempts_StateShape",
                $"([Status] = {(int)FulfillmentAttemptStatus.Processing} " +
                "AND [CompletedAtUtc] IS NULL " +
                "AND [FailureDisposition] IS NULL " +
                "AND [FailureCode] IS NULL " +
                "AND [FailureMessage] IS NULL) OR " +
                $"([Status] = {(int)FulfillmentAttemptStatus.Succeeded} " +
                "AND [CompletedAtUtc] IS NOT NULL " +
                "AND [FailureDisposition] IS NULL " +
                "AND [FailureCode] IS NULL " +
                "AND [FailureMessage] IS NULL) OR " +
                $"([Status] = {(int)FulfillmentAttemptStatus.Failed} " +
                "AND [CompletedAtUtc] IS NOT NULL " +
                "AND [FailureDisposition] IS NOT NULL " +
                "AND [FailureCode] IS NOT NULL)");
        });
    }
}
