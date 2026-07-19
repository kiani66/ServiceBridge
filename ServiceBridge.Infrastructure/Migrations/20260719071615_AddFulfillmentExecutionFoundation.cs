using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFulfillmentExecutionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FulfillmentExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastAttemptNumber = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FulfillmentExecutions", x => x.Id);
                    table.CheckConstraint("CK_FulfillmentExecutions_LastAttemptNumber_NonNegative", "[LastAttemptNumber] >= 0");
                    table.CheckConstraint("CK_FulfillmentExecutions_StateShape", "([Status] = 1 AND [CompletedAtUtc] IS NULL AND (([LastAttemptNumber] = 0 AND [StartedAtUtc] IS NULL) OR ([LastAttemptNumber] > 0 AND [StartedAtUtc] IS NOT NULL))) OR ([Status] = 2 AND [LastAttemptNumber] > 0 AND [StartedAtUtc] IS NOT NULL AND [CompletedAtUtc] IS NULL) OR ([Status] IN (3, 4) AND [LastAttemptNumber] > 0 AND [StartedAtUtc] IS NOT NULL AND [CompletedAtUtc] IS NOT NULL AND [CompletedAtUtc] >= [StartedAtUtc])");
                    table.CheckConstraint("CK_FulfillmentExecutions_Status_Valid", "[Status] IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_FulfillmentExecutions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FulfillmentAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FulfillmentExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    FulfillmentMode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureDisposition = table.Column<int>(type: "int", nullable: true),
                    FailureCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FulfillmentAttempts", x => x.Id);
                    table.CheckConstraint("CK_FulfillmentAttempts_AttemptNumber_Positive", "[AttemptNumber] > 0");
                    table.CheckConstraint("CK_FulfillmentAttempts_CompletionChronology", "[CompletedAtUtc] IS NULL OR [CompletedAtUtc] >= [StartedAtUtc]");
                    table.CheckConstraint("CK_FulfillmentAttempts_FailureCode_NonEmpty", "[FailureCode] IS NULL OR LEN([FailureCode]) > 0");
                    table.CheckConstraint("CK_FulfillmentAttempts_FailureDisposition_Valid", "[FailureDisposition] IS NULL OR [FailureDisposition] IN (1, 2)");
                    table.CheckConstraint("CK_FulfillmentAttempts_FulfillmentMode_Valid", "[FulfillmentMode] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_FulfillmentAttempts_StateShape", "([Status] = 1 AND [CompletedAtUtc] IS NULL AND [FailureDisposition] IS NULL AND [FailureCode] IS NULL AND [FailureMessage] IS NULL) OR ([Status] = 2 AND [CompletedAtUtc] IS NOT NULL AND [FailureDisposition] IS NULL AND [FailureCode] IS NULL AND [FailureMessage] IS NULL) OR ([Status] = 3 AND [CompletedAtUtc] IS NOT NULL AND [FailureDisposition] IS NOT NULL AND [FailureCode] IS NOT NULL)");
                    table.CheckConstraint("CK_FulfillmentAttempts_Status_Valid", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_FulfillmentAttempts_FulfillmentExecutions_FulfillmentExecutionId",
                        column: x => x.FulfillmentExecutionId,
                        principalTable: "FulfillmentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_FulfillmentAttempts_ExecutionId_AttemptNumber",
                table: "FulfillmentAttempts",
                columns: new[] { "FulfillmentExecutionId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_FulfillmentExecutions_OrderId",
                table: "FulfillmentExecutions",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FulfillmentAttempts");

            migrationBuilder.DropTable(
                name: "FulfillmentExecutions");
        }
    }
}
