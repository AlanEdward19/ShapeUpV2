using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Nutrition.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NutritionDiaryDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionDiaryDays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutritionGoalEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    GoalMet = table.Column<bool>(type: "bit", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionGoalEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutritionProfiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    HeightCm = table.Column<int>(type: "int", nullable: true),
                    Age = table.Column<int>(type: "int", nullable: true),
                    BiologicalSex = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ActivityLevel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    OnboardingSkipped = table.Column<bool>(type: "bit", nullable: false),
                    ActiveGoal_Kcal = table.Column<int>(type: "int", nullable: true),
                    ActiveGoal_ProteinG = table.Column<int>(type: "int", nullable: true),
                    ActiveGoal_CarbG = table.Column<int>(type: "int", nullable: true),
                    ActiveGoal_FatG = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionProfiles", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "NutritionWeightRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionWeightRegisters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutritionWeightTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TargetWeight = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionWeightTargets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutritionDiaryEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    DiaryDayId = table.Column<int>(type: "int", nullable: false),
                    MealSlot = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FoodId = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    UsesOverride = table.Column<bool>(type: "bit", nullable: false),
                    QuantityGramsOrMl = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ComputedMacros_Kcal = table.Column<int>(type: "int", nullable: false),
                    ComputedMacros_ProteinG = table.Column<int>(type: "int", nullable: false),
                    ComputedMacros_CarbG = table.Column<int>(type: "int", nullable: false),
                    ComputedMacros_FatG = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionDiaryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NutritionDiaryEntries_NutritionDiaryDays_DiaryDayId",
                        column: x => x.DiaryDayId,
                        principalTable: "NutritionDiaryDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionDiaryDays_UserId_Date",
                table: "NutritionDiaryDays",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NutritionDiaryEntries_DiaryDayId",
                table: "NutritionDiaryEntries",
                column: "DiaryDayId");

            migrationBuilder.CreateIndex(
                name: "IX_NutritionGoalEvaluations_UserId_Date",
                table: "NutritionGoalEvaluations",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NutritionWeightRegisters_UserId_Date",
                table: "NutritionWeightRegisters",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NutritionWeightTargets_UserId",
                table: "NutritionWeightTargets",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NutritionDiaryEntries");

            migrationBuilder.DropTable(
                name: "NutritionGoalEvaluations");

            migrationBuilder.DropTable(
                name: "NutritionProfiles");

            migrationBuilder.DropTable(
                name: "NutritionWeightRegisters");

            migrationBuilder.DropTable(
                name: "NutritionWeightTargets");

            migrationBuilder.DropTable(
                name: "NutritionDiaryDays");
        }
    }
}
