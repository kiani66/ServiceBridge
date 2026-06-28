using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "DigitalServices",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(170)", maxLength: 170, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Slug",
                table: "Providers",
                column: "Slug",
                unique: true);

            migrationBuilder.Sql(
                """
                WITH ProviderNames AS
                (
                    SELECT DISTINCT
                        ProviderName = CASE
                            WHEN NULLIF(LTRIM(RTRIM([Provider])), '') IS NULL
                                THEN N'Unknown Provider'
                            ELSE LTRIM(RTRIM([Provider]))
                        END
                    FROM [DigitalServices]
                ),
                ProviderSlugs AS
                (
                    SELECT
                        ProviderName,
                        Slug = CASE
                            WHEN NULLIF(NormalizedSlug, '') IS NULL
                                THEN N'unknown-provider'
                            ELSE NormalizedSlug
                        END
                    FROM ProviderNames
                    CROSS APPLY
                    (
                        SELECT RawSlug = LOWER(
                            REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                                ProviderName,
                                N'&', N' and '),
                                N'.', N''),
                                N',', N''),
                                N'''', N''),
                                N'/', N'-'),
                                N'\', N'-'),
                                N' ', N'-'))
                    ) raw
                    CROSS APPLY
                    (
                        SELECT NormalizedSlug = REPLACE(REPLACE(REPLACE(REPLACE(
                            raw.RawSlug,
                            N'----', N'-'),
                            N'---', N'-'),
                            N'--', N'-'),
                            N'--', N'-')
                    ) normalized
                )
                INSERT INTO [Providers] ([Id], [Name], [Slug], [IsActive], [CreatedAt], [UpdatedAt])
                SELECT NEWID(), MIN([ProviderName]), [Slug], CAST(1 AS bit), SYSUTCDATETIME(), NULL
                FROM ProviderSlugs
                GROUP BY [Slug];
                """);

            migrationBuilder.Sql(
                """
                UPDATE service
                SET [ProviderId] = provider.[Id]
                FROM [DigitalServices] service
                CROSS APPLY
                (
                    SELECT ProviderName = CASE
                        WHEN NULLIF(LTRIM(RTRIM(service.[Provider])), '') IS NULL
                            THEN N'Unknown Provider'
                        ELSE LTRIM(RTRIM(service.[Provider]))
                    END
                ) providerName
                CROSS APPLY
                (
                    SELECT RawSlug = LOWER(
                        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                            providerName.ProviderName,
                            N'&', N' and '),
                            N'.', N''),
                            N',', N''),
                            N'''', N''),
                            N'/', N'-'),
                            N'\', N'-'),
                            N' ', N'-'))
                ) raw
                CROSS APPLY
                (
                    SELECT Slug = CASE
                        WHEN NULLIF(REPLACE(REPLACE(REPLACE(REPLACE(
                            raw.RawSlug,
                            N'----', N'-'),
                            N'---', N'-'),
                            N'--', N'-'),
                            N'--', N'-'), '') IS NULL
                            THEN N'unknown-provider'
                        ELSE REPLACE(REPLACE(REPLACE(REPLACE(
                            raw.RawSlug,
                            N'----', N'-'),
                            N'---', N'-'),
                            N'--', N'-'),
                            N'--', N'-')
                    END
                ) slug
                INNER JOIN [Providers] provider ON provider.[Slug] = slug.[Slug];
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM [DigitalServices]
                    WHERE [ProviderId] = '00000000-0000-0000-0000-000000000000'
                )
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM [Providers] WHERE [Slug] = N'unknown-provider')
                    BEGIN
                        INSERT INTO [Providers] ([Id], [Name], [Slug], [IsActive], [CreatedAt], [UpdatedAt])
                        VALUES (NEWID(), N'Unknown Provider', N'unknown-provider', CAST(1 AS bit), SYSUTCDATETIME(), NULL);
                    END

                    UPDATE [DigitalServices]
                    SET [ProviderId] = (SELECT TOP(1) [Id] FROM [Providers] WHERE [Slug] = N'unknown-provider')
                    WHERE [ProviderId] = '00000000-0000-0000-0000-000000000000';
                END
                """);

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "DigitalServices");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalServices_ProviderId",
                table: "DigitalServices",
                column: "ProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_DigitalServices_Providers_ProviderId",
                table: "DigitalServices",
                column: "ProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "DigitalServices",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE service
                SET [Provider] = provider.[Name]
                FROM [DigitalServices] service
                INNER JOIN [Providers] provider ON provider.[Id] = service.[ProviderId];
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_DigitalServices_Providers_ProviderId",
                table: "DigitalServices");

            migrationBuilder.DropIndex(
                name: "IX_DigitalServices_ProviderId",
                table: "DigitalServices");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "DigitalServices");

            migrationBuilder.DropTable(
                name: "Providers");
        }
    }
}
