using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Gamification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreditGranted = table.Column<bool>(type: "bit", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluations", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TotalXp = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false),
                    LastActivityDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShapeCoins = table.Column<int>(type: "int", nullable: false),
                    LastStreakMilestoneAwarded = table.Column<int>(type: "int", nullable: false),
                    LastEvaluationLeveledUp = table.Column<bool>(type: "bit", nullable: false),
                    LastEvaluationLevelFrom = table.Column<int>(type: "int", nullable: true),
                    LastEvaluationLevelTo = table.Column<int>(type: "int", nullable: true),
                    LastEvaluationStreakMilestoneHit = table.Column<bool>(type: "bit", nullable: false),
                    LastEvaluationStreakMilestoneValue = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_UserId_EvaluatedAtUtc",
                table: "Evaluations",
                columns: new[] { "UserId", "EvaluatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Evaluations");

            migrationBuilder.DropTable(
                name: "Profiles");
        }
    }
}
