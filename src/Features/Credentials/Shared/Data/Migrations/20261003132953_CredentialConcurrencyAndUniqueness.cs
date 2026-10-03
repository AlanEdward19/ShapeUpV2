using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Credentials.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class CredentialConcurrencyAndUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EndReason",
                table: "ProfessionalCredentials",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "ProfessionalCredentials",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndedByUserId",
                table: "ProfessionalCredentials",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProfessionalCredentials",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ProfessionalCredentials_OpenPerRegistration",
                table: "ProfessionalCredentials");

            migrationBuilder.DropIndex(
                name: "UX_ProfessionalCredentials_OpenPerUserAndProfession",
                table: "ProfessionalCredentials");

            migrationBuilder.DropColumn(
                name: "EndReason",
                table: "ProfessionalCredentials");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "ProfessionalCredentials");

            migrationBuilder.DropColumn(
                name: "EndedByUserId",
                table: "ProfessionalCredentials");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProfessionalCredentials");
        }
    }
}
