using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Nutrition.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWaterGoalToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WaterGoalMl",
                table: "NutritionProfiles",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaterGoalMl",
                table: "NutritionProfiles");
        }
    }
}
