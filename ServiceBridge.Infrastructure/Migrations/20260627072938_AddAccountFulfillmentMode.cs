using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountFulfillmentMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceRequiredFields_ServiceId_FieldKey",
                table: "ServiceRequiredFields");

            migrationBuilder.DropIndex(
                name: "IX_OrderFieldValues_OrderId_FieldKey",
                table: "OrderFieldValues");

            migrationBuilder.AddColumn<int>(
                name: "Actor",
                table: "ServiceRequiredFields",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "FulfillmentMode",
                table: "ServiceRequiredFields",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisibleToCustomerAfterCompletion",
                table: "ServiceRequiredFields",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FulfillmentMode",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Actor",
                table: "OrderFieldValues",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "OrderFieldValues",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FulfillmentMode",
                table: "OrderFieldValues",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisibleToCustomerAfterCompletion",
                table: "OrderFieldValues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsCreateNewAccount",
                table: "DigitalServices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsUseExistingAccount",
                table: "DigitalServices",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequiredFields_ServiceId_FulfillmentMode_Actor_FieldKey",
                table: "ServiceRequiredFields",
                columns: new[] { "ServiceId", "FulfillmentMode", "Actor", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderFieldValues_OrderId_FulfillmentMode_Actor_FieldKey",
                table: "OrderFieldValues",
                columns: new[] { "OrderId", "FulfillmentMode", "Actor", "FieldKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceRequiredFields_ServiceId_FulfillmentMode_Actor_FieldKey",
                table: "ServiceRequiredFields");

            migrationBuilder.DropIndex(
                name: "IX_OrderFieldValues_OrderId_FulfillmentMode_Actor_FieldKey",
                table: "OrderFieldValues");

            migrationBuilder.DropColumn(
                name: "Actor",
                table: "ServiceRequiredFields");

            migrationBuilder.DropColumn(
                name: "FulfillmentMode",
                table: "ServiceRequiredFields");

            migrationBuilder.DropColumn(
                name: "IsVisibleToCustomerAfterCompletion",
                table: "ServiceRequiredFields");

            migrationBuilder.DropColumn(
                name: "FulfillmentMode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Actor",
                table: "OrderFieldValues");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "OrderFieldValues");

            migrationBuilder.DropColumn(
                name: "FulfillmentMode",
                table: "OrderFieldValues");

            migrationBuilder.DropColumn(
                name: "IsVisibleToCustomerAfterCompletion",
                table: "OrderFieldValues");

            migrationBuilder.DropColumn(
                name: "SupportsCreateNewAccount",
                table: "DigitalServices");

            migrationBuilder.DropColumn(
                name: "SupportsUseExistingAccount",
                table: "DigitalServices");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequiredFields_ServiceId_FieldKey",
                table: "ServiceRequiredFields",
                columns: new[] { "ServiceId", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderFieldValues_OrderId_FieldKey",
                table: "OrderFieldValues",
                columns: new[] { "OrderId", "FieldKey" },
                unique: true);
        }
    }
}
