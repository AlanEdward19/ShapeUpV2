using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Nutrition.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFastingRecommendationLifecycleAndEarlyEnd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecommendationDeclineReason",
                table: "NutritionFastingAgendas",
                type: "nvarchar(280)",
                maxLength: 280,
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "RecommendationDeclinedAtUtc",
                table: "NutritionFastingAgendas",
                type: "datetime2",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "RecommendationStatus",
                table: "NutritionFastingAgendas",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "RecommendedAtUtc",
                table: "NutritionFastingAgendas",
                type: "datetime2",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "RecommendedByUserId",
                table: "NutritionFastingAgendas",
                type: "int",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "EndReason",
                table: "NutritionFastingOverrides",
                type: "nvarchar(280)",
                maxLength: 280,
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "EndedEarlyAtUtc",
                table: "NutritionFastingOverrides",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecommendationDeclineReason",
                table: "NutritionFastingAgendas");

            migrationBuilder.DropColumn(
                name: "RecommendationDeclinedAtUtc",
                table: "NutritionFastingAgendas");

            migrationBuilder.DropColumn(
                name: "RecommendationStatus",
                table: "NutritionFastingAgendas");

            migrationBuilder.DropColumn(
                name: "RecommendedAtUtc",
                table: "NutritionFastingAgendas");

            migrationBuilder.DropColumn(
                name: "RecommendedByUserId",
                table: "NutritionFastingAgendas");

            migrationBuilder.DropColumn(
                name: "EndReason",
                table: "NutritionFastingOverrides");

            migrationBuilder.DropColumn(
                name: "EndedEarlyAtUtc",
                table: "NutritionFastingOverrides");
        }
    }
}
