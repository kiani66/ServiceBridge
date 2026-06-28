using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanDurationDaysSnapshot",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanNameSnapshot",
                table: "Orders",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanRegionCodeSnapshot",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServicePlanId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServicePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(170)", maxLength: 170, nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: true),
                    RegionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ProcessingTimeDescription = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePlans", x => x.Id);
                    table.CheckConstraint("CK_ServicePlans_BasePrice_NonNegative", "[BasePrice] >= 0");
                    table.CheckConstraint("CK_ServicePlans_DurationDays_Positive", "[DurationDays] IS NULL OR [DurationDays] > 0");
                    table.ForeignKey(
                        name: "FK_ServicePlans_DigitalServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "DigitalServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ServicePlanId",
                table: "Orders",
                column: "ServicePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePlans_ServiceId_IsActive_DisplayOrder",
                table: "ServicePlans",
                columns: new[] { "ServiceId", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePlans_ServiceId_Slug",
                table: "ServicePlans",
                columns: new[] { "ServiceId", "Slug" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ServicePlans_ServicePlanId",
                table: "Orders",
                column: "ServicePlanId",
                principalTable: "ServicePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ServicePlans_ServicePlanId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "ServicePlans");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ServicePlanId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PlanDurationDaysSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PlanNameSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PlanRegionCodeSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ServicePlanId",
                table: "Orders");
        }
    }
}
