using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImplementOrderWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Status_Valid",
                table: "Payments");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                newName: "IX_Payments_OrderId_Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentAssignments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentAssignments_Users_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentAssignments_Users_AssignedByAdminId",
                        column: x => x.AssignedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerActionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CustomerResponse = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerActionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerActionRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerActionRequests_Users_RequestedByAgentId",
                        column: x => x.RequestedByAgentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    GatewayReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentEvents_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId_Completed",
                table: "Payments",
                column: "OrderId",
                unique: true,
                filter: "[Status] = 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Status_Valid",
                table: "Payments",
                sql: "[Status] IN (1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_AgentAssignments_AgentId",
                table: "AgentAssignments",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentAssignments_AssignedByAdminId",
                table: "AgentAssignments",
                column: "AssignedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentAssignments_OrderId_AssignedAt",
                table: "AgentAssignments",
                columns: new[] { "OrderId", "AssignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerActionRequests_OrderId_IsResolved",
                table: "CustomerActionRequests",
                columns: new[] { "OrderId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerActionRequests_RequestedByAgentId_IsResolved",
                table: "CustomerActionRequests",
                columns: new[] { "RequestedByAgentId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentEvents_PaymentId_CreatedAt",
                table: "PaymentEvents",
                columns: new[] { "PaymentId", "CreatedAt" });

            migrationBuilder.Sql(
                """
                INSERT INTO AgentAssignments
                    (Id, OrderId, AgentId, AssignedByAdminId, AssignedAt, Notes, CreatedAt, UpdatedAt)
                SELECT
                    NEWID(),
                    Id,
                    AssignedAgentId,
                    NULL,
                    COALESCE(AssignedAt, CreatedAt),
                    N'Migrated from the legacy order assignment fields.',
                    COALESCE(AssignedAt, CreatedAt),
                    NULL
                FROM Orders
                WHERE AssignedAgentId IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO PaymentEvents
                    (Id, PaymentId, EventType, GatewayReference, Payload, Note, CreatedAt, UpdatedAt)
                SELECT
                    NEWID(),
                    Id,
                    1,
                    NULL,
                    NULL,
                    N'Migrated payment initiation event.',
                    CreatedAt,
                    NULL
                FROM Payments;

                INSERT INTO PaymentEvents
                    (Id, PaymentId, EventType, GatewayReference, Payload, Note, CreatedAt, UpdatedAt)
                SELECT
                    NEWID(),
                    Id,
                    CASE Status WHEN 2 THEN 2 WHEN 3 THEN 3 WHEN 4 THEN 4 END,
                    GatewayReference,
                    CallbackPayload,
                    N'Migrated terminal payment event.',
                    COALESCE(ConfirmedAt, FailedAt, RefundedAt, UpdatedAt, CreatedAt),
                    NULL
                FROM Payments
                WHERE Status IN (2, 3, 4);
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO OrderStatusHistory
                    (Id, OrderId, PreviousStatus, NewStatus, ActorType, ActorId,
                     ChangedByUserId, Note, CreatedAt, UpdatedAt)
                SELECT
                    NEWID(),
                    o.Id,
                    NULL,
                    1,
                    1,
                    o.UserId,
                    o.UserId,
                    N'Migrated initial Created status.',
                    DATEADD(millisecond, -1, o.CreatedAt),
                    NULL
                FROM Orders o
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM OrderStatusHistory h
                    WHERE h.OrderId = o.Id AND h.NewStatus = 1
                );

                INSERT INTO CustomerActionRequests
                    (Id, OrderId, RequestedByAgentId, RequestText, CustomerResponse,
                     RequestedAt, RespondedAt, IsResolved, CreatedAt, UpdatedAt)
                SELECT
                    NEWID(),
                    Id,
                    AssignedAgentId,
                    N'Customer action required (migrated).',
                    NULL,
                    COALESCE(UpdatedAt, CreatedAt),
                    NULL,
                    0,
                    COALESCE(UpdatedAt, CreatedAt),
                    NULL
                FROM Orders
                WHERE Status = 6 AND AssignedAgentId IS NOT NULL;

                UPDATE Orders
                SET Status = 5
                WHERE Status = 6;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE o
                SET Status = 6
                FROM Orders o
                WHERE EXISTS
                (
                    SELECT 1
                    FROM CustomerActionRequests r
                    WHERE r.OrderId = o.Id
                      AND r.RequestText = N'Customer action required (migrated).'
                );

                DELETE FROM OrderStatusHistory
                WHERE Note = N'Migrated initial Created status.'
                  AND PreviousStatus IS NULL
                  AND NewStatus = 1;
                """);

            migrationBuilder.DropTable(
                name: "AgentAssignments");

            migrationBuilder.DropTable(
                name: "CustomerActionRequests");

            migrationBuilder.DropTable(
                name: "PaymentEvents");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderId_Completed",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Status_Valid",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "Payments");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_OrderId_Pending",
                table: "Payments",
                newName: "IX_Payments_OrderId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Status_Valid",
                table: "Payments",
                sql: "[Status] IN (1, 2, 3)");

        }
    }
}
