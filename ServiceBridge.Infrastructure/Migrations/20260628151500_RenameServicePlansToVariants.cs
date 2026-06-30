using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ServiceBridge.Infrastructure.Data;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ServiceBridgeDbContext))]
    [Migration("20260628151500_RenameServicePlansToVariants")]
    public partial class RenameServicePlansToVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ServicePlans_ServicePlanId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicePlans_DigitalServices_ServiceId",
                table: "ServicePlans");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ServicePlanId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServicePlans",
                table: "ServicePlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServicePlans_BasePrice_NonNegative",
                table: "ServicePlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServicePlans_DurationDays_Positive",
                table: "ServicePlans");

            migrationBuilder.RenameTable(
                name: "ServicePlans",
                newName: "ServiceVariants");

            migrationBuilder.RenameColumn(
                name: "ServicePlanId",
                table: "Orders",
                newName: "ServiceVariantId");

            migrationBuilder.RenameColumn(
                name: "PlanNameSnapshot",
                table: "Orders",
                newName: "VariantNameSnapshot");

            migrationBuilder.RenameColumn(
                name: "PlanDurationDaysSnapshot",
                table: "Orders",
                newName: "VariantDurationDaysSnapshot");

            migrationBuilder.RenameColumn(
                name: "PlanRegionCodeSnapshot",
                table: "Orders",
                newName: "VariantRegionCodeSnapshot");

            migrationBuilder.RenameIndex(
                name: "IX_ServicePlans_ServiceId_Slug",
                table: "ServiceVariants",
                newName: "IX_ServiceVariants_ServiceId_Slug");

            migrationBuilder.RenameIndex(
                name: "IX_ServicePlans_ServiceId_IsActive_DisplayOrder",
                table: "ServiceVariants",
                newName: "IX_ServiceVariants_ServiceId_IsActive_DisplayOrder");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServiceVariants",
                table: "ServiceVariants",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServiceVariants_BasePrice_NonNegative",
                table: "ServiceVariants",
                sql: "[BasePrice] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServiceVariants_DurationDays_Positive",
                table: "ServiceVariants",
                sql: "[DurationDays] IS NULL OR [DurationDays] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ServiceVariantId",
                table: "Orders",
                column: "ServiceVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceVariants_DigitalServices_ServiceId",
                table: "ServiceVariants",
                column: "ServiceId",
                principalTable: "DigitalServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ServiceVariants_ServiceVariantId",
                table: "Orders",
                column: "ServiceVariantId",
                principalTable: "ServiceVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ServiceVariants_ServiceVariantId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceVariants_DigitalServices_ServiceId",
                table: "ServiceVariants");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ServiceVariantId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServiceVariants",
                table: "ServiceVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServiceVariants_BasePrice_NonNegative",
                table: "ServiceVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServiceVariants_DurationDays_Positive",
                table: "ServiceVariants");

            migrationBuilder.RenameTable(
                name: "ServiceVariants",
                newName: "ServicePlans");

            migrationBuilder.RenameColumn(
                name: "ServiceVariantId",
                table: "Orders",
                newName: "ServicePlanId");

            migrationBuilder.RenameColumn(
                name: "VariantNameSnapshot",
                table: "Orders",
                newName: "PlanNameSnapshot");

            migrationBuilder.RenameColumn(
                name: "VariantDurationDaysSnapshot",
                table: "Orders",
                newName: "PlanDurationDaysSnapshot");

            migrationBuilder.RenameColumn(
                name: "VariantRegionCodeSnapshot",
                table: "Orders",
                newName: "PlanRegionCodeSnapshot");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceVariants_ServiceId_Slug",
                table: "ServicePlans",
                newName: "IX_ServicePlans_ServiceId_Slug");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceVariants_ServiceId_IsActive_DisplayOrder",
                table: "ServicePlans",
                newName: "IX_ServicePlans_ServiceId_IsActive_DisplayOrder");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServicePlans",
                table: "ServicePlans",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServicePlans_BasePrice_NonNegative",
                table: "ServicePlans",
                sql: "[BasePrice] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServicePlans_DurationDays_Positive",
                table: "ServicePlans",
                sql: "[DurationDays] IS NULL OR [DurationDays] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ServicePlanId",
                table: "Orders",
                column: "ServicePlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServicePlans_DigitalServices_ServiceId",
                table: "ServicePlans",
                column: "ServiceId",
                principalTable: "DigitalServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ServicePlans_ServicePlanId",
                table: "Orders",
                column: "ServicePlanId",
                principalTable: "ServicePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
