using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShapeUp.Features.PlatformFeatureFlags.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedFeatureHealthFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PlatformFeatureFlags",
                columns: new[] { "Key", "Enabled", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { "features.gamification", true, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "features.gym-management", true, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "features.notifications", true, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "features.nutrition", true, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "features.training", true, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PlatformFeatureFlags",
                keyColumn: "Key",
                keyValue: "features.gamification");

            migrationBuilder.DeleteData(
                table: "PlatformFeatureFlags",
                keyColumn: "Key",
                keyValue: "features.gym-management");

            migrationBuilder.DeleteData(
                table: "PlatformFeatureFlags",
                keyColumn: "Key",
                keyValue: "features.notifications");

            migrationBuilder.DeleteData(
                table: "PlatformFeatureFlags",
                keyColumn: "Key",
                keyValue: "features.nutrition");

            migrationBuilder.DeleteData(
                table: "PlatformFeatureFlags",
                keyColumn: "Key",
                keyValue: "features.training");
        }
    }
}
