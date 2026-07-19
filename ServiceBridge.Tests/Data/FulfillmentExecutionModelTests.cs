using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.ValueObjects;
using ServiceBridge.Infrastructure.Data;
using Xunit;

namespace ServiceBridge.Tests.Data;

public sealed class FulfillmentExecutionModelTests
{
    private readonly ServiceBridgeDbContext _context;

    public FulfillmentExecutionModelTests()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=ServiceBridgeFulfillmentExecutionModelTests")
            .Options;

        _context = new ServiceBridgeDbContext(options);
    }

    [Fact]
    public void IdentifiersAreClientGeneratedAndExecutionUsesRowVersion()
    {
        var executionType = _context.Model.FindEntityType(typeof(FulfillmentExecution))!;
        var attemptType = _context.Model.FindEntityType(typeof(FulfillmentAttempt))!;
        var rowVersion = executionType.FindProperty(
            nameof(FulfillmentExecution.RowVersion))!;

        Assert.Equal(
            ValueGenerated.Never,
            executionType.FindProperty(nameof(FulfillmentExecution.Id))!.ValueGenerated);
        Assert.Equal(
            ValueGenerated.Never,
            attemptType.FindProperty(nameof(FulfillmentAttempt.Id))!.ValueGenerated);
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }

    [Fact]
    public void ExecutionOrderIndexIsUnique()
    {
        var entityType = _context.Model.FindEntityType(typeof(FulfillmentExecution))!;
        var index = Assert.Single(entityType.GetIndexes(), candidate =>
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(FulfillmentExecution.OrderId) }));

        Assert.True(index.IsUnique);
        Assert.Equal("UX_FulfillmentExecutions_OrderId", index.GetDatabaseName());
    }

    [Fact]
    public void AttemptSequenceIndexIsUniquePerExecution()
    {
        var entityType = _context.Model.FindEntityType(typeof(FulfillmentAttempt))!;
        var index = Assert.Single(entityType.GetIndexes(), candidate =>
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual(new[]
                {
                    nameof(FulfillmentAttempt.FulfillmentExecutionId),
                    nameof(FulfillmentAttempt.AttemptNumber)
                }));

        Assert.True(index.IsUnique);
        Assert.Equal(
            "UX_FulfillmentAttempts_ExecutionId_AttemptNumber",
            index.GetDatabaseName());
    }

    [Fact]
    public void RelationshipsUseRequiredForeignKeysAndRestrictDelete()
    {
        var executionType = _context.Model.FindEntityType(typeof(FulfillmentExecution))!;
        var attemptType = _context.Model.FindEntityType(typeof(FulfillmentAttempt))!;
        var orderForeignKey = Assert.Single(executionType.GetForeignKeys());
        var executionForeignKey = Assert.Single(attemptType.GetForeignKeys());

        Assert.True(orderForeignKey.IsRequired);
        Assert.True(orderForeignKey.IsUnique);
        Assert.Equal(typeof(Order), orderForeignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, orderForeignKey.DeleteBehavior);
        Assert.Equal(
            new[] { nameof(FulfillmentExecution.OrderId) },
            orderForeignKey.Properties.Select(property => property.Name));

        Assert.True(executionForeignKey.IsRequired);
        Assert.Equal(
            typeof(FulfillmentExecution),
            executionForeignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, executionForeignKey.DeleteBehavior);
        Assert.Equal(
            new[] { nameof(FulfillmentAttempt.FulfillmentExecutionId) },
            executionForeignKey.Properties.Select(property => property.Name));
    }

    [Fact]
    public void EnumsArePersistedAsRequiredIntegers()
    {
        var executionType = _context.Model.FindEntityType(typeof(FulfillmentExecution))!;
        var attemptType = _context.Model.FindEntityType(typeof(FulfillmentAttempt))!;

        AssertIntegerEnum<FulfillmentExecutionStatus>(
            executionType,
            nameof(FulfillmentExecution.Status),
            nullable: false);
        AssertIntegerEnum<FulfillmentMode>(
            attemptType,
            nameof(FulfillmentAttempt.FulfillmentMode),
            nullable: false);
        AssertIntegerEnum<FulfillmentAttemptStatus>(
            attemptType,
            nameof(FulfillmentAttempt.Status),
            nullable: false);
        AssertIntegerEnum<FulfillmentFailureDisposition?>(
            attemptType,
            nameof(FulfillmentAttempt.FailureDisposition),
            nullable: true);
    }

    [Fact]
    public void FailureColumnsAreBoundedAndCodeUsesStrongType()
    {
        var entityType = _context.Model.FindEntityType(typeof(FulfillmentAttempt))!;
        var failureCode = entityType.FindProperty(nameof(FulfillmentAttempt.FailureCode))!;
        var failureMessage = entityType.FindProperty(
            nameof(FulfillmentAttempt.FailureMessage))!;

        Assert.Equal(typeof(FulfillmentFailureCode), failureCode.ClrType);
        Assert.Equal(FulfillmentFailureCode.MaxLength, failureCode.GetMaxLength());
        Assert.False(failureCode.IsUnicode());
        Assert.Equal(
            FulfillmentAttempt.FailureMessageMaxLength,
            failureMessage.GetMaxLength());
    }

    [Fact]
    public void ExecutionAndAttemptConstraintsProtectEnumsAndStateShapes()
    {
        var designTimeModel = _context.GetService<IDesignTimeModel>().Model;
        var executionConstraints = designTimeModel
            .FindEntityType(typeof(FulfillmentExecution))!
            .GetCheckConstraints()
            .ToDictionary(constraint => constraint.Name!, constraint => NormalizeSql(constraint.Sql));
        var attemptConstraints = designTimeModel
            .FindEntityType(typeof(FulfillmentAttempt))!
            .GetCheckConstraints()
            .ToDictionary(constraint => constraint.Name!, constraint => NormalizeSql(constraint.Sql));

        Assert.Equal(
            "[STATUS]IN(1,2,3,4)",
            executionConstraints["CK_FulfillmentExecutions_Status_Valid"]);
        Assert.Contains(
            "[LASTATTEMPTNUMBER]>=0",
            executionConstraints["CK_FulfillmentExecutions_LastAttemptNumber_NonNegative"]);
        Assert.Contains(
            "[STATUS]=1",
            executionConstraints["CK_FulfillmentExecutions_StateShape"]);
        Assert.Contains(
            "[STATUS]IN(3,4)",
            executionConstraints["CK_FulfillmentExecutions_StateShape"]);

        Assert.Equal(
            "[ATTEMPTNUMBER]>0",
            attemptConstraints["CK_FulfillmentAttempts_AttemptNumber_Positive"]);
        Assert.Equal(
            "[FULFILLMENTMODE]IN(1,2,3,4)",
            attemptConstraints["CK_FulfillmentAttempts_FulfillmentMode_Valid"]);
        Assert.Equal(
            "[STATUS]IN(1,2,3)",
            attemptConstraints["CK_FulfillmentAttempts_Status_Valid"]);
        Assert.Contains(
            "[FAILUREDISPOSITION]ISNULLOR[FAILUREDISPOSITION]IN(1,2)",
            attemptConstraints["CK_FulfillmentAttempts_FailureDisposition_Valid"]);
        Assert.Equal(
            "[FAILURECODE]ISNULLORLEN([FAILURECODE])>0",
            attemptConstraints["CK_FulfillmentAttempts_FailureCode_NonEmpty"]);
        Assert.Contains(
            "[STATUS]=1AND[COMPLETEDATUTC]ISNULL",
            attemptConstraints["CK_FulfillmentAttempts_StateShape"]);
        Assert.Contains(
            "[STATUS]=2AND[COMPLETEDATUTC]ISNOTNULL",
            attemptConstraints["CK_FulfillmentAttempts_StateShape"]);
        Assert.Contains(
            "[STATUS]=3AND[COMPLETEDATUTC]ISNOTNULL",
            attemptConstraints["CK_FulfillmentAttempts_StateShape"]);
    }

    private static void AssertIntegerEnum<TEnum>(
        IEntityType entityType,
        string propertyName,
        bool nullable)
    {
        var property = entityType.FindProperty(propertyName)!;

        Assert.Equal(nullable, property.IsNullable);
        Assert.Equal(typeof(TEnum), property.ClrType);
        Assert.Equal("int", property.GetColumnType());
    }

    private static string NormalizeSql(string sql)
    {
        return Regex.Replace(sql, "\\s+", string.Empty).ToUpperInvariant();
    }
}
