using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Credentials.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfessionalCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProfessionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CredentialNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IssuingAuthority = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IssuingRegion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalCredentials_UserId",
                table: "ProfessionalCredentials",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalCredentials_UserId_ProfessionType_Status",
                table: "ProfessionalCredentials",
                columns: new[] { "UserId", "ProfessionType", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfessionalCredentials");
        }
    }
}
