using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;
using System.Text.RegularExpressions;
using Xunit;

namespace ServiceBridge.Tests.Data;

public class FulfillmentPolicyModelTests
{
    private readonly ServiceBridgeDbContext _context;

    public FulfillmentPolicyModelTests()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=ServiceBridgeFulfillmentPolicyModelTests")
            .Options;

        _context = new ServiceBridgeDbContext(options);
    }

    [Fact]
    public void FulfillmentPolicy_UsesClientGeneratedIdAndExplicitIntegerEnums()
    {
        var entityType = _context.Model.FindEntityType(typeof(FulfillmentPolicy))!;

        Assert.Equal(
            ValueGenerated.Never,
            entityType.FindProperty(nameof(FulfillmentPolicy.Id))!.ValueGenerated);

        AssertIntegerEnum<ProcurementStrategy>(entityType, nameof(FulfillmentPolicy.ProcurementStrategy));
        AssertIntegerEnum<FulfillmentMode>(entityType, nameof(FulfillmentPolicy.FulfillmentMode));
        AssertIntegerEnum<DeliveryArtifactType>(entityType, nameof(FulfillmentPolicy.DeliveryArtifactType));
    }

    [Fact]
    public void FulfillmentPolicy_TargetVersionIndexIsUnique()
    {
        var entityType = _context.Model.FindEntityType(typeof(FulfillmentPolicy))!;

        var index = Assert.Single(entityType.GetIndexes(), index =>
            index.Properties.Select(x => x.Name).SequenceEqual(new[]
            {
                nameof(FulfillmentPolicy.ServiceVariantId),
                nameof(FulfillmentPolicy.Version)
            }));

        Assert.True(index.IsUnique);
        Assert.Equal(
            "UX_FulfillmentPolicies_ServiceVariantId_Version",
            index.GetDatabaseName());
    }

    [Fact]
    public void FulfillmentPolicy_TargetRelationshipUsesRestrictDeleteBehavior()
    {
        var entityType = _context.Model.FindEntityType(typeof(FulfillmentPolicy))!;
        var foreignKey = Assert.Single(entityType.GetForeignKeys());

        Assert.Equal(typeof(ServiceVariant), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal(
            new[] { nameof(FulfillmentPolicy.ServiceVariantId) },
            foreignKey.Properties.Select(x => x.Name));
    }

    [Fact]
    public void FulfillmentPolicy_HasVersionAndEnumValidityConstraints()
    {
        var designTimeModel = _context.GetService<IDesignTimeModel>().Model;
        var entityType = designTimeModel.FindEntityType(typeof(FulfillmentPolicy))!;
        var constraints = entityType.GetCheckConstraints()
            .ToDictionary(x => x.Name!, x => NormalizeSql(x.Sql));
        var validProcurementStrategyValues = string.Join(",", Enum
            .GetValues<ProcurementStrategy>()
            .Select(value => (int)value));

        Assert.Equal(
            "[VERSION]>0",
            constraints["CK_FulfillmentPolicies_Version_Positive"]);
        Assert.Equal(
            $"[PROCUREMENTSTRATEGY]IN({validProcurementStrategyValues})",
            constraints["CK_FulfillmentPolicies_ProcurementStrategy_Valid"]);
        Assert.Equal(
            "[FULFILLMENTMODE]IN(1,2,3,4)",
            constraints["CK_FulfillmentPolicies_FulfillmentMode_Valid"]);
        Assert.Equal(
            "[DELIVERYARTIFACTTYPE]IN(1,2,3,4)",
            constraints["CK_FulfillmentPolicies_DeliveryArtifactType_Valid"]);
    }

    private static void AssertIntegerEnum<TEnum>(
        IEntityType entityType,
        string propertyName)
        where TEnum : struct, Enum
    {
        var property = entityType.FindProperty(propertyName)!;

        Assert.False(property.IsNullable);
        Assert.Equal(typeof(TEnum), property.ClrType);
        Assert.Equal("int", property.GetColumnType());
    }

    private static string NormalizeSql(string sql)
    {
        return Regex.Replace(sql, "\\s+", string.Empty).ToUpperInvariant();
    }
}
