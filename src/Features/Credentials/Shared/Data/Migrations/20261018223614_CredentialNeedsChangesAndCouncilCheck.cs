using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Credentials.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class CredentialNeedsChangesAndCouncilCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ProfessionalCredentials_OpenPerRegistration",
                table: "ProfessionalCredentials");

            migrationBuilder.DropIndex(
                name: "UX_ProfessionalCredentials_OpenPerUserAndProfession",
                table: "ProfessionalCredentials");

            migrationBuilder.AddColumn<DateTime>(
                name: "ChangesRequestedAt",
                table: "ProfessionalCredentials",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangesRequestedMessage",
                table: "ProfessionalCredentials",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCouncilCheckAt",
                table: "ProfessionalCredentials",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CredentialVerificationAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CredentialId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CredentialVerificationAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ProfessionalCredentials_OpenPerRegistration",
                table: "ProfessionalCredentials",
                columns: new[] { "IssuingAuthority", "IssuingRegion", "CredentialNumber" },
                unique: true,
                filter: "[Status] IN (1, 2, 3, 8)");

            migrationBuilder.CreateIndex(
                name: "UX_ProfessionalCredentials_OpenPerUserAndProfession",
                table: "ProfessionalCredentials",
                columns: new[] { "UserId", "ProfessionType" },
                unique: true,
                filter: "[Status] IN (1, 2, 3, 8)");

            migrationBuilder.CreateIndex(
                name: "IX_CredentialVerificationAudits_CredentialId",
                table: "CredentialVerificationAudits",
                column: "CredentialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CredentialVerificationAudits");

            migrationBuilder.DropIndex(
                name: "UX_ProfessionalCredentials_OpenPerRegistration",
                table: "ProfessionalCredentials");

            migrationBuilder.DropIndex(
                name: "UX_ProfessionalCredentials_OpenPerUserAndProfession",
                table: "ProfessionalCredentials");

            migrationBuilder.DropColumn(
                name: "ChangesRequestedAt",
                table: "ProfessionalCredentials");

            migrationBuilder.DropColumn(
                name: "ChangesRequestedMessage",
                table: "ProfessionalCredentials");

            migrationBuilder.DropColumn(
                name: "LastCouncilCheckAt",
                table: "ProfessionalCredentials");

            migrationBuilder.CreateIndex(
                name: "UX_ProfessionalCredentials_OpenPerRegistration",
                table: "ProfessionalCredentials",
                columns: new[] { "IssuingAuthority", "IssuingRegion", "CredentialNumber" },
                unique: true,
                filter: "[Status] IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "UX_ProfessionalCredentials_OpenPerUserAndProfession",
                table: "ProfessionalCredentials",
                columns: new[] { "UserId", "ProfessionType" },
                unique: true,
                filter: "[Status] IN (1, 2, 3)");
        }
    }
}
