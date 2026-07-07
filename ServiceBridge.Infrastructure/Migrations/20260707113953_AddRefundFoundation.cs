using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefundRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundRequests", x => x.Id);
                    table.CheckConstraint("CK_RefundRequests_Amount_Positive", "[Amount] > 0");
                    table.CheckConstraint("CK_RefundRequests_Status_Valid", "[Status] IN (1, 2, 3, 4, 5)");
                    table.ForeignKey(
                        name: "FK_RefundRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundRequests_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundRequests_Users_ReviewedByAdminId",
                        column: x => x.ReviewedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefundRequestStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefundRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ActorType = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundRequestStatusHistory", x => x.Id);
                    table.CheckConstraint("CK_RefundRequestStatusHistory_NewStatus_Valid", "[NewStatus] IN (1, 2, 3, 4, 5)");
                    table.ForeignKey(
                        name: "FK_RefundRequestStatusHistory_RefundRequests_RefundRequestId",
                        column: x => x.RefundRequestId,
                        principalTable: "RefundRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_OrderId_PaymentId_Status",
                table: "RefundRequests",
                columns: new[] { "OrderId", "PaymentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_PaymentId",
                table: "RefundRequests",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_RequestedByUserId",
                table: "RefundRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_ReviewedByAdminId",
                table: "RefundRequests",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequestStatusHistory_RefundRequestId",
                table: "RefundRequestStatusHistory",
                column: "RefundRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefundRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "RefundRequests");
        }
    }
}
