using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    RateToIrr = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.CheckConstraint("CK_ExchangeRates_RateToIrr_Positive", "[RateToIrr] > 0");
                });

            migrationBuilder.CreateTable(
                name: "ProfitMargins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitMargins", x => x.Id);
                    table.CheckConstraint("CK_ProfitMargins_Percentage_Range", "[Percentage] >= 0 AND [Percentage] <= 100");
                    table.CheckConstraint("CK_ProfitMargins_SingleScope", "NOT ([CategoryId] IS NOT NULL AND [ServiceId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ProfitMargins_DigitalServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "DigitalServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfitMargins_ServiceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_CurrencyCode_IsActive_EffectiveFrom",
                table: "ExchangeRates",
                columns: new[] { "CurrencyCode", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitMargins_CategoryId_IsActive_EffectiveFrom",
                table: "ProfitMargins",
                columns: new[] { "CategoryId", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitMargins_IsActive_EffectiveFrom",
                table: "ProfitMargins",
                columns: new[] { "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitMargins_ServiceId_IsActive_EffectiveFrom",
                table: "ProfitMargins",
                columns: new[] { "ServiceId", "IsActive", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "ProfitMargins");
        }
    }
}
