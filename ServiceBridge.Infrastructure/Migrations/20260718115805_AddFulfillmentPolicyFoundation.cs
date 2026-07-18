using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFulfillmentPolicyFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FulfillmentPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ProcurementStrategy = table.Column<int>(type: "int", nullable: false),
                    FulfillmentMode = table.Column<int>(type: "int", nullable: false),
                    DeliveryArtifactType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FulfillmentPolicies", x => x.Id);
                    table.CheckConstraint("CK_FulfillmentPolicies_DeliveryArtifactType_Valid", "[DeliveryArtifactType] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_FulfillmentPolicies_FulfillmentMode_Valid", "[FulfillmentMode] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_FulfillmentPolicies_ProcurementStrategy_Valid", "[ProcurementStrategy] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_FulfillmentPolicies_Version_Positive", "[Version] > 0");
                    table.ForeignKey(
                        name: "FK_FulfillmentPolicies_ServiceVariants_ServiceVariantId",
                        column: x => x.ServiceVariantId,
                        principalTable: "ServiceVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_FulfillmentPolicies_ServiceVariantId_Version",
                table: "FulfillmentPolicies",
                columns: new[] { "ServiceVariantId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FulfillmentPolicies");
        }
    }
}
