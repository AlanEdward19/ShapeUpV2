using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Credentials.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ProfessionalCredentials",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "ProfessionalCredentials",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByUserId",
                table: "ProfessionalCredentials",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "ProfessionalCredentials",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalCredentials_Status",
                table: "ProfessionalCredentials",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProfessionalCredentials_Status",
                table: "ProfessionalCredentials");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ProfessionalCredentials");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ProfessionalCredentials");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "ProfessionalCredentials");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "ProfessionalCredentials");
        }
    }
}
