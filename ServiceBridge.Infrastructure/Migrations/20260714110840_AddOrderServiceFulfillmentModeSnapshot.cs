using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderServiceFulfillmentModeSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceFulfillmentModeSnapshot",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: (int)ServiceBridge.Domain.Enums.FulfillmentMode.ManualAgent);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ServiceFulfillmentModeSnapshot_Valid",
                table: "Orders",
                sql: "[ServiceFulfillmentModeSnapshot] IN (1, 2, 3, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ServiceFulfillmentModeSnapshot_Valid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ServiceFulfillmentModeSnapshot",
                table: "Orders");
        }
    }
}
