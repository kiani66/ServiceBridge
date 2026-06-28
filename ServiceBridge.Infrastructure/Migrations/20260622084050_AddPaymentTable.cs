using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ChangedByUserId",
                table: "OrderStatusHistory",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid?>(
                name: "ActorId",
                table: "OrderStatusHistory",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "ActorType",
                table: "OrderStatusHistory",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [OrderStatusHistory]
                SET [ActorId] = [ChangedByUserId],
                    [ActorType] = 1
                WHERE [ActorId] IS NULL
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ActorId",
                table: "OrderStatusHistory",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ActorType",
                table: "OrderStatusHistory",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountIRR = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    AmountUSD = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ExchangeRateSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GatewayReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CallbackPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amounts_Positive", "[AmountIRR] > 0 AND [AmountUSD] > 0 AND [ExchangeRateSnapshot] > 0");
                    table.CheckConstraint("CK_Payments_Status_Valid", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderStatusHistory_ActorType_Valid",
                table: "OrderStatusHistory",
                sql: "[ActorType] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId",
                unique: true,
                filter: "[Status] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderStatusHistory_ActorType_Valid",
                table: "OrderStatusHistory");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "OrderStatusHistory");

            migrationBuilder.DropColumn(
                name: "ActorType",
                table: "OrderStatusHistory");

            migrationBuilder.AlterColumn<Guid>(
                name: "ChangedByUserId",
                table: "OrderStatusHistory",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
