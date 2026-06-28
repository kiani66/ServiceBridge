using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminOrderAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderStatusHistory_ActorType_Valid",
                table: "OrderStatusHistory");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderStatusHistory_ActorType_Valid",
                table: "OrderStatusHistory",
                sql: "[ActorType] IN (1, 2, 3, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderStatusHistory_ActorType_Valid",
                table: "OrderStatusHistory");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderStatusHistory_ActorType_Valid",
                table: "OrderStatusHistory",
                sql: "[ActorType] IN (1, 2)");
        }
    }
}
