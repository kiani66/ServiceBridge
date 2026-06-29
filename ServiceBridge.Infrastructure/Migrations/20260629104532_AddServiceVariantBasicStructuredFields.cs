using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountDeliveryType",
                table: "ServiceVariants");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "ServiceVariants");
        }
    }
}
