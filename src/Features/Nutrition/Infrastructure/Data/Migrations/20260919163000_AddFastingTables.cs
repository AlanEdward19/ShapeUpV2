using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Nutrition.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFastingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NutritionFastingAgendas",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FastHours = table.Column<int>(type: "int", nullable: true),
                    EatHours = table.Column<int>(type: "int", nullable: true),
                    Protocol = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    EatingStartMinutes = table.Column<int>(type: "int", nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RecommendedFastHours = table.Column<int>(type: "int", nullable: true),
                    RecommendedProtocol = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionFastingAgendas", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "NutritionFastingOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    FastHours = table.Column<int>(type: "int", nullable: false),
                    EatHours = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FastEndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EatEndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionFastingOverrides", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionFastingOverrides_UserId",
                table: "NutritionFastingOverrides",
                column: "UserId",
                unique: true,
                filter: "[Status] IN ('Fasting', 'Eating')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NutritionFastingAgendas");

            migrationBuilder.DropTable(
                name: "NutritionFastingOverrides");
        }
    }
}
