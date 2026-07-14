using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Migrations;
using Xunit;

namespace ServiceBridge.Tests.Migrations;

public class OrderServiceFulfillmentModeSnapshotMigrationTests
{
    [Fact]
    public void Up_BackfillsExistingOrdersAsManualAgentWithoutReadingServiceVariants()
    {
        var migrationBuilder = BuildUpMigration();

        var snapshotColumn = Assert.Single(migrationBuilder.Operations
            .OfType<AddColumnOperation>()
            .Where(operation => operation.Name == "ServiceFulfillmentModeSnapshot"));

        Assert.Equal("Orders", snapshotColumn.Table);
        Assert.False(snapshotColumn.IsNullable);
        Assert.Equal((int)FulfillmentMode.ManualAgent, snapshotColumn.DefaultValue);
        Assert.Empty(migrationBuilder.Operations.OfType<SqlOperation>());
    }

    [Fact]
    public void Up_AddsServiceFulfillmentModeSnapshotValidityConstraint()
    {
        var migrationBuilder = BuildUpMigration();

        var constraint = Assert.Single(migrationBuilder.Operations
            .OfType<AddCheckConstraintOperation>()
            .Where(operation => operation.Name ==
                                "CK_Orders_ServiceFulfillmentModeSnapshot_Valid"));

        Assert.Equal("Orders", constraint.Table);
        Assert.Equal(
            "[ServiceFulfillmentModeSnapshot] IN (1, 2, 3, 4)",
            constraint.Sql);
    }

    private static MigrationBuilder BuildUpMigration()
    {
        var migration = new AddOrderServiceFulfillmentModeSnapshot();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var upMethod = migration.GetType().GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        upMethod.Invoke(migration, [migrationBuilder]);

        return migrationBuilder;
    }
}
