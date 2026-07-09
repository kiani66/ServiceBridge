using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceTicketSupportV1Enums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [Tickets] SET [Status] = 5 WHERE [Status] = 4;");

            migrationBuilder.Sql(
                "UPDATE [Tickets] SET [Priority] = 3 WHERE [Priority] = 4;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_Priority_Valid",
                table: "Tickets",
                sql: "[Priority] IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_Status_Valid",
                table: "Tickets",
                sql: "[Status] IN (1, 2, 3, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_Priority_Valid",
                table: "Tickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_Status_Valid",
                table: "Tickets");
        }
    }
}
