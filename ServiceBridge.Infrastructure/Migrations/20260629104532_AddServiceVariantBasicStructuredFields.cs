using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ServiceBridge.Infrastructure.Data;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ServiceBridgeDbContext))]
    [Migration("20260629104532_AddServiceVariantBasicStructuredFields")]
    public partial class AddServiceVariantBasicStructuredFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountDeliveryType",
                table: "ServiceVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BillingCycle",
                table: "ServiceVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServiceVariants_BillingCycle_Valid",
                table: "ServiceVariants",
                sql: "[BillingCycle] IN (0, 1, 2, 3, 4, 5, 6)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServiceVariants_AccountDeliveryType_Valid",
                table: "ServiceVariants",
                sql: "[AccountDeliveryType] IN (0, 1, 2, 3, 4, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ServiceVariants_AccountDeliveryType_Valid",
                table: "ServiceVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServiceVariants_BillingCycle_Valid",
                table: "ServiceVariants");

            migrationBuilder.DropColumn(
                name: "AccountDeliveryType",
                table: "ServiceVariants");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "ServiceVariants");
        }
    }
}
