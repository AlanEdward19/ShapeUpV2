using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Training.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseEquivalents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseEquivalents",
                columns: table => new
                {
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    EquivalentExerciseId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseEquivalents", x => new { x.ExerciseId, x.EquivalentExerciseId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseEquivalents_EquivalentExerciseId",
                table: "ExerciseEquivalents",
                column: "EquivalentExerciseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseEquivalents");
        }
    }
}
