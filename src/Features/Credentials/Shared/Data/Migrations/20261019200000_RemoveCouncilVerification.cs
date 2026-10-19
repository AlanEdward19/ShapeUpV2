using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Credentials.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCouncilVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CredentialVerificationAudits");

            migrationBuilder.DropColumn(
                name: "LastCouncilCheckAt",
                table: "ProfessionalCredentials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_CredentialVerificationAudits_CredentialId",
                table: "CredentialVerificationAudits",
                column: "CredentialId");
        }
    }
}
