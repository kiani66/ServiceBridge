using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteServiceVariantFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [ServiceVariants] " +
                "SET [RegionCode] = N'GLOBAL' " +
                "WHERE [RegionCode] IS NULL OR LTRIM(RTRIM([RegionCode])) = N''");

            migrationBuilder.Sql(
                "UPDATE [ServiceVariants] " +
                "SET [CurrencyCode] = N'USD' " +
                "WHERE LTRIM(RTRIM([CurrencyCode])) = N''");

            migrationBuilder.AlterColumn<string>(
                name: "RegionCode",
                table: "ServiceVariants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "GLOBAL",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CurrencyCode",
                table: "ServiceVariants",
                type: "nchar(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "USD",
                oldClrType: typeof(string),
                oldType: "nchar(3)",
                oldFixedLength: true,
                oldMaxLength: 3);

            migrationBuilder.AddColumn<int>(
                name: "FulfillmentMode",
                table: "ServiceVariants",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServiceVariants_FulfillmentMode_Valid",
                table: "ServiceVariants",
                sql: "[FulfillmentMode] IN (1, 2, 3, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ServiceVariants_FulfillmentMode_Valid",
                table: "ServiceVariants");

            migrationBuilder.DropColumn(
                name: "FulfillmentMode",
                table: "ServiceVariants");

            migrationBuilder.AlterColumn<string>(
                name: "RegionCode",
                table: "ServiceVariants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "GLOBAL");

            migrationBuilder.AlterColumn<string>(
                name: "CurrencyCode",
                table: "ServiceVariants",
                type: "nchar(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nchar(3)",
                oldFixedLength: true,
                oldMaxLength: 3,
                oldDefaultValue: "USD");
        }
    }
}
