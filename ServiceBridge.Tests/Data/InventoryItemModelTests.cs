using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;
using System.Text.RegularExpressions;
using Xunit;

namespace ServiceBridge.Tests.Data;

public class InventoryItemModelTests
{
    private readonly ServiceBridgeDbContext _context;

    public InventoryItemModelTests()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=ServiceBridgeInventoryModelTests")
            .Options;

        _context = new ServiceBridgeDbContext(options);
    }

    [Fact]
    public void InventoryItem_UsesClientGeneratedIdAndRowVersion()
    {
        var entityType = _context.Model.FindEntityType(typeof(InventoryItem))!;
        var id = entityType.FindProperty(nameof(InventoryItem.Id))!;
        var rowVersion = entityType.FindProperty(nameof(InventoryItem.RowVersion))!;

        Assert.Equal(ValueGenerated.Never, id.ValueGenerated);
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }

    [Fact]
    public void InventoryItem_CodeFingerprintIndexIsUnique()
    {
        var entityType = _context.Model.FindEntityType(typeof(InventoryItem))!;

        var index = Assert.Single(entityType.GetIndexes(), index =>
            index.Properties.Select(x => x.Name)
                .SequenceEqual(new[] { nameof(InventoryItem.CodeFingerprint) }));

        Assert.True(index.IsUnique);
        Assert.Equal("UX_InventoryItems_CodeFingerprint", index.GetDatabaseName());
    }

    [Fact]
    public void InventoryItem_ProtectedValueAndFingerprintUseExpectedMaximumLengths()
    {
        var entityType = _context.Model.FindEntityType(typeof(InventoryItem))!;

        Assert.Equal(
            2048,
            entityType.FindProperty(nameof(InventoryItem.ProtectedCode))!.GetMaxLength());
        Assert.Equal(
            64,
            entityType.FindProperty(nameof(InventoryItem.CodeFingerprint))!.GetMaxLength());
    }

    [Fact]
    public void InventoryItem_ReservedOrderIndexIsFilteredAndUnique()
    {
        var entityType = _context.Model.FindEntityType(typeof(InventoryItem))!;

        var index = Assert.Single(entityType.GetIndexes(), index =>
            index.Properties.Select(x => x.Name)
                .SequenceEqual(new[] { nameof(InventoryItem.ReservedForOrderId) }));

        Assert.True(index.IsUnique);
        Assert.Equal("[ReservedForOrderId] IS NOT NULL", index.GetFilter());
    }

    [Fact]
    public void InventoryItem_AllocationIndexUsesRequiredColumnOrder()
    {
        var entityType = _context.Model.FindEntityType(typeof(InventoryItem))!;

        Assert.Contains(entityType.GetIndexes(), index =>
            index.GetDatabaseName() == "IX_InventoryItems_Allocation" &&
            index.Properties.Select(x => x.Name).SequenceEqual(new[]
            {
                nameof(InventoryItem.ServiceVariantId),
                nameof(InventoryItem.Status),
                nameof(InventoryItem.CreatedAt),
                nameof(InventoryItem.Id)
            }));
    }

    [Fact]
    public void InventoryItem_HasStatusAndStateShapeConstraints()
    {
        var designTimeModel = _context.GetService<IDesignTimeModel>().Model;
        var entityType = designTimeModel.FindEntityType(typeof(InventoryItem))!;
        var constraints = entityType.GetCheckConstraints().ToArray();
        var statusConstraint = constraints.Single(x =>
            x.Name == "CK_InventoryItems_Status_Valid");
        var stateShapeConstraint = constraints.Single(x =>
            x.Name == "CK_InventoryItems_StateShape");

        Assert.Equal("[STATUS]IN(1,2,3,4)", NormalizeSql(statusConstraint.Sql));

        var stateShape = NormalizeSql(stateShapeConstraint.Sql);
        Assert.Contains(
            "[STATUS]=1AND[RESERVEDFORORDERID]ISNULLAND[RESERVEDATUTC]ISNULLAND[DELIVEREDATUTC]ISNULLAND[DISABLEDBYADMINID]ISNULLAND[DISABLEDATUTC]ISNULL",
            stateShape);
        Assert.Contains(
            "[STATUS]=2AND[RESERVEDFORORDERID]ISNOTNULLAND[RESERVEDATUTC]ISNOTNULLAND[DELIVEREDATUTC]ISNULLAND[DISABLEDBYADMINID]ISNULLAND[DISABLEDATUTC]ISNULL",
            stateShape);
        Assert.Contains(
            "[STATUS]=3AND[RESERVEDFORORDERID]ISNOTNULLAND[RESERVEDATUTC]ISNOTNULLAND[DELIVEREDATUTC]ISNOTNULLAND[DISABLEDBYADMINID]ISNULLAND[DISABLEDATUTC]ISNULL",
            stateShape);
        Assert.Contains(
            "[STATUS]=4AND[RESERVEDFORORDERID]ISNULLAND[RESERVEDATUTC]ISNULLAND[DELIVEREDATUTC]ISNULLAND[DISABLEDBYADMINID]ISNOTNULLAND[DISABLEDATUTC]ISNOTNULL",
            stateShape);
    }

    [Fact]
    public void InventoryItem_OperationalRelationshipsUseRestrictDeleteBehavior()
    {
        var entityType = _context.Model.FindEntityType(typeof(InventoryItem))!;
        var foreignKeys = entityType.GetForeignKeys().ToArray();

        Assert.Equal(4, foreignKeys.Length);
        Assert.All(foreignKeys, foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Contains(foreignKeys, foreignKey =>
            foreignKey.Properties.Select(x => x.Name)
                .SequenceEqual(new[] { nameof(InventoryItem.ServiceVariantId) }));
        Assert.Contains(foreignKeys, foreignKey =>
            foreignKey.Properties.Select(x => x.Name)
                .SequenceEqual(new[] { nameof(InventoryItem.ReservedForOrderId) }));
        Assert.Contains(foreignKeys, foreignKey =>
            foreignKey.Properties.Select(x => x.Name)
                .SequenceEqual(new[] { nameof(InventoryItem.CreatedByAdminId) }));
        Assert.Contains(foreignKeys, foreignKey =>
            foreignKey.Properties.Select(x => x.Name)
                .SequenceEqual(new[] { nameof(InventoryItem.DisabledByAdminId) }));
    }

    private static string NormalizeSql(string sql)
    {
        return Regex.Replace(sql, "\\s+", string.Empty).ToUpperInvariant();
    }
}
