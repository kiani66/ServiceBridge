using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProtectedCode = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CodeFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservedForOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisabledByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisabledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.CheckConstraint("CK_InventoryItems_StateShape", "([Status] = 1 AND [ReservedForOrderId] IS NULL AND [ReservedAtUtc] IS NULL AND [DeliveredAtUtc] IS NULL AND [DisabledByAdminId] IS NULL AND [DisabledAtUtc] IS NULL) OR ([Status] = 2 AND [ReservedForOrderId] IS NOT NULL AND [ReservedAtUtc] IS NOT NULL AND [DeliveredAtUtc] IS NULL AND [DisabledByAdminId] IS NULL AND [DisabledAtUtc] IS NULL) OR ([Status] = 3 AND [ReservedForOrderId] IS NOT NULL AND [ReservedAtUtc] IS NOT NULL AND [DeliveredAtUtc] IS NOT NULL AND [DisabledByAdminId] IS NULL AND [DisabledAtUtc] IS NULL) OR ([Status] = 4 AND [ReservedForOrderId] IS NULL AND [ReservedAtUtc] IS NULL AND [DeliveredAtUtc] IS NULL AND [DisabledByAdminId] IS NOT NULL AND [DisabledAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_InventoryItems_Status_Valid", "[Status] IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_InventoryItems_Orders_ReservedForOrderId",
                        column: x => x.ReservedForOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_ServiceVariants_ServiceVariantId",
                        column: x => x.ServiceVariantId,
                        principalTable: "ServiceVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Users_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Users_DisabledByAdminId",
                        column: x => x.DisabledByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Allocation",
                table: "InventoryItems",
                columns: new[] { "ServiceVariantId", "Status", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_CreatedByAdminId",
                table: "InventoryItems",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_DisabledByAdminId",
                table: "InventoryItems",
                column: "DisabledByAdminId");

            migrationBuilder.CreateIndex(
                name: "UX_InventoryItems_CodeFingerprint",
                table: "InventoryItems",
                column: "CodeFingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_InventoryItems_ReservedForOrderId",
                table: "InventoryItems",
                column: "ReservedForOrderId",
                unique: true,
                filter: "[ReservedForOrderId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryItems");
        }
    }
}
