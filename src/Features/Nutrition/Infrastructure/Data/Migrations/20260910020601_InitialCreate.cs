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
                name: "DiaryDays",
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
                    table.PrimaryKey("PK_DiaryDays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoalEvaluations",
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
                    table.PrimaryKey("PK_GoalEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
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
                    table.PrimaryKey("PK_Profiles", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "WeightRegisters",
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
                    table.PrimaryKey("PK_WeightRegisters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeightTargets",
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
                    table.PrimaryKey("PK_WeightTargets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiaryEntries",
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
                    table.PrimaryKey("PK_DiaryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiaryEntries_DiaryDays_DiaryDayId",
                        column: x => x.DiaryDayId,
                        principalTable: "DiaryDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiaryDays_UserId_Date",
                table: "DiaryDays",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiaryEntries_DiaryDayId",
                table: "DiaryEntries",
                column: "DiaryDayId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalEvaluations_UserId_Date",
                table: "GoalEvaluations",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightRegisters_UserId_Date",
                table: "WeightRegisters",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightTargets_UserId",
                table: "WeightTargets",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiaryEntries");

            migrationBuilder.DropTable(
                name: "GoalEvaluations");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "WeightRegisters");

            migrationBuilder.DropTable(
                name: "WeightTargets");

            migrationBuilder.DropTable(
                name: "DiaryDays");
        }
    }
}
