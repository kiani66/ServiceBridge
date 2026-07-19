using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderFulfillmentSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryArtifactTypeSnapshot",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FulfillmentPolicyId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FulfillmentPolicyVersionSnapshot",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcurementStrategySnapshot",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PolicyFulfillmentModeSnapshot",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_FulfillmentPolicyId",
                table: "Orders",
                column: "FulfillmentPolicyId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_FulfillmentPolicySnapshot_Complete",
                table: "Orders",
                sql: "([FulfillmentPolicyId] IS NULL AND [FulfillmentPolicyVersionSnapshot] IS NULL AND [ProcurementStrategySnapshot] IS NULL AND [PolicyFulfillmentModeSnapshot] IS NULL AND [DeliveryArtifactTypeSnapshot] IS NULL) OR ([FulfillmentPolicyId] IS NOT NULL AND [FulfillmentPolicyVersionSnapshot] IS NOT NULL AND [FulfillmentPolicyVersionSnapshot] > 0 AND [ProcurementStrategySnapshot] IS NOT NULL AND [ProcurementStrategySnapshot] IN (1, 2, 3, 4, 5) AND [PolicyFulfillmentModeSnapshot] IS NOT NULL AND [PolicyFulfillmentModeSnapshot] IN (1, 2, 3, 4) AND [DeliveryArtifactTypeSnapshot] IS NOT NULL AND [DeliveryArtifactTypeSnapshot] IN (1, 2, 3, 4))");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_FulfillmentPolicies_FulfillmentPolicyId",
                table: "Orders",
                column: "FulfillmentPolicyId",
                principalTable: "FulfillmentPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_FulfillmentPolicies_FulfillmentPolicyId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_FulfillmentPolicyId",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_FulfillmentPolicySnapshot_Complete",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryArtifactTypeSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FulfillmentPolicyId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FulfillmentPolicyVersionSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProcurementStrategySnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PolicyFulfillmentModeSnapshot",
                table: "Orders");
        }
    }
}
