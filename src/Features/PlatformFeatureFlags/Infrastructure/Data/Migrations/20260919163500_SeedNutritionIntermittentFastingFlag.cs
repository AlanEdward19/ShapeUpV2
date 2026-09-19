using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.PlatformFeatureFlags.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedNutritionIntermittentFastingFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PlatformFeatureFlags",
                columns: new[] { "Key", "Enabled", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { "nutrition.intermittent-fasting", true, new DateTime(2026, 9, 19, 0, 0, 0, 0, DateTimeKind.Utc), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PlatformFeatureFlags",
                keyColumn: "Key",
                keyValue: "nutrition.intermittent-fasting");
        }
    }
}
