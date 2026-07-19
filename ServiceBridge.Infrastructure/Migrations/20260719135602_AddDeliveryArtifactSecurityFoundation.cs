using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryArtifactSecurityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_FulfillmentExecutions_Id_OrderId",
                table: "FulfillmentExecutions",
                columns: new[] { "Id", "OrderId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_FulfillmentAttempts_Id_FulfillmentExecutionId",
                table: "FulfillmentAttempts",
                columns: new[] { "Id", "FulfillmentExecutionId" });

            migrationBuilder.CreateTable(
                name: "DeliveryArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FulfillmentExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FulfillmentAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtifactType = table.Column<int>(type: "int", nullable: false),
                    ProtectedValue = table.Column<string>(type: "nvarchar(max)", maxLength: 32768, nullable: false),
                    Fingerprint = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryArtifacts", x => x.Id);
                    table.CheckConstraint("CK_DeliveryArtifacts_ArtifactType_Valid", "[ArtifactType] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_DeliveryArtifacts_ProtectedValue_Bounded", "LEN([ProtectedValue]) > 0 AND LEN([ProtectedValue]) <= 32768");
                    table.ForeignKey(
                        name: "FK_DeliveryArtifacts_FulfillmentAttempts_FulfillmentAttemptId_FulfillmentExecutionId",
                        columns: x => new { x.FulfillmentAttemptId, x.FulfillmentExecutionId },
                        principalTable: "FulfillmentAttempts",
                        principalColumns: new[] { "Id", "FulfillmentExecutionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryArtifacts_FulfillmentExecutions_FulfillmentExecutionId_OrderId",
                        columns: x => new { x.FulfillmentExecutionId, x.OrderId },
                        principalTable: "FulfillmentExecutions",
                        principalColumns: new[] { "Id", "OrderId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryArtifacts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryArtifacts_FulfillmentAttemptId",
                table: "DeliveryArtifacts",
                column: "FulfillmentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryArtifacts_FulfillmentAttemptId_FulfillmentExecutionId",
                table: "DeliveryArtifacts",
                columns: new[] { "FulfillmentAttemptId", "FulfillmentExecutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryArtifacts_FulfillmentExecutionId_OrderId",
                table: "DeliveryArtifacts",
                columns: new[] { "FulfillmentExecutionId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryArtifacts_OrderId",
                table: "DeliveryArtifacts",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryArtifacts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FulfillmentExecutions_Id_OrderId",
                table: "FulfillmentExecutions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FulfillmentAttempts_Id_FulfillmentExecutionId",
                table: "FulfillmentAttempts");
        }
    }
}
