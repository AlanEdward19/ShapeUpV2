using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Relationships.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfessionalClientRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessionalUserId = table.Column<int>(type: "int", nullable: false),
                    ClientUserId = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalClientRelationships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalClientRelationships_ClientUserId",
                table: "ProfessionalClientRelationships",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalClientRelationships_ProfessionalUserId_ClientUserId_RelationshipType",
                table: "ProfessionalClientRelationships",
                columns: new[] { "ProfessionalUserId", "ClientUserId", "RelationshipType" },
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfessionalClientRelationships");
        }
    }
}
