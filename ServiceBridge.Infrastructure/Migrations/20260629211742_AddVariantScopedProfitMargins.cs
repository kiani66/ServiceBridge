using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantScopedProfitMargins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ProfitMargins_SingleScope",
                table: "ProfitMargins");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceVariantId",
                table: "ProfitMargins",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfitMargins_ServiceVariantId_IsActive_EffectiveFrom",
                table: "ProfitMargins",
                columns: new[] { "ServiceVariantId", "IsActive", "EffectiveFrom" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProfitMargins_SingleScope",
                table: "ProfitMargins",
                sql: "([ServiceVariantId] IS NULL AND [ServiceId] IS NULL AND [CategoryId] IS NULL) OR ([ServiceVariantId] IS NOT NULL AND [ServiceId] IS NULL AND [CategoryId] IS NULL) OR ([ServiceVariantId] IS NULL AND [ServiceId] IS NOT NULL AND [CategoryId] IS NULL) OR ([ServiceVariantId] IS NULL AND [ServiceId] IS NULL AND [CategoryId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitMargins_ServiceVariants_ServiceVariantId",
                table: "ProfitMargins",
                column: "ServiceVariantId",
                principalTable: "ServiceVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfitMargins_ServiceVariants_ServiceVariantId",
                table: "ProfitMargins");

            migrationBuilder.DropIndex(
                name: "IX_ProfitMargins_ServiceVariantId_IsActive_EffectiveFrom",
                table: "ProfitMargins");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProfitMargins_SingleScope",
                table: "ProfitMargins");

            migrationBuilder.DropColumn(
                name: "ServiceVariantId",
                table: "ProfitMargins");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProfitMargins_SingleScope",
                table: "ProfitMargins",
                sql: "NOT ([CategoryId] IS NOT NULL AND [ServiceId] IS NOT NULL)");
        }
    }
}
