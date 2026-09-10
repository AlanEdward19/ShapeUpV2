using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Gamification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionGamificationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "LastNutritionGoalMetDate",
                table: "Profiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NutritionCurrentStreak",
                table: "Profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "NutritionEvaluations",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CreditedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionEvaluations", x => new { x.UserId, x.Date });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NutritionEvaluations");

            migrationBuilder.DropColumn(
                name: "LastNutritionGoalMetDate",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "NutritionCurrentStreak",
                table: "Profiles");
        }
    }
}
