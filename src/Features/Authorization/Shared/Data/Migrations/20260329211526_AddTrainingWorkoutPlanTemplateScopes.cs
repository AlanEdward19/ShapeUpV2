using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShapeUp.Features.Authorization.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingWorkoutPlanTemplateScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Scopes",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Domain", "Name", "Subdomain" },
                values: new object[,]
                {
                    { 59, "create", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Create workout plans", "training", "training:workout-plans:create", "workout_plans" },
                    { 60, "read", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Read workout plans", "training", "training:workout-plans:read", "workout_plans" },
                    { 61, "copy", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Copy workout plans", "training", "training:workout-plans:copy", "workout_plans" },
                    { 62, "create", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Create workout templates", "training", "training:workout-templates:create", "workout_templates" },
                    { 63, "read", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Read workout templates", "training", "training:workout-templates:read", "workout_templates" },
                    { 64, "copy", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Copy workout templates", "training", "training:workout-templates:copy", "workout_templates" },
                    { 65, "assign", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Assign workout templates to users", "training", "training:workout-templates:assign", "workout_templates" },
                    { 66, "start", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Start workout executions", "training", "training:workouts:start", "workouts" },
                    { 67, "update", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Update workout execution state", "training", "training:workouts:update", "workouts" },
                    { 68, "finish", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Finish workout executions", "training", "training:workouts:finish", "workouts" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 68);
        }
    }
}
