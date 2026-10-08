using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Nutrition.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMealsAndDiaryMealLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MealId",
                table: "NutritionDiaryEntries",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MealName",
                table: "NutritionDiaryEntries",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NutritionUserMeals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Time = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    WeekDays = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TargetProteinG = table.Column<int>(type: "int", nullable: true),
                    TargetCarbG = table.Column<int>(type: "int", nullable: true),
                    TargetFatG = table.Column<int>(type: "int", nullable: true),
                    ItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionUserMeals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionUserMeals_UserId_SortOrder",
                table: "NutritionUserMeals",
                columns: new[] { "UserId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NutritionUserMeals");

            migrationBuilder.DropColumn(
                name: "MealId",
                table: "NutritionDiaryEntries");

            migrationBuilder.DropColumn(
                name: "MealName",
                table: "NutritionDiaryEntries");
        }
    }
}
