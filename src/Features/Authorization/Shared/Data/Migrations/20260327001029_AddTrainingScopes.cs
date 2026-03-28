using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShapeUp.Features.Authorization.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Scopes",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Domain", "Name", "Subdomain" },
                values: new object[,]
                {
                    { 9, "create", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Create exercises", "training", "training:exercises:create", "exercises" },
                    { 10, "read", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Read exercises", "training", "training:exercises:read", "exercises" },
                    { 11, "update", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Update exercises", "training", "training:exercises:update", "exercises" },
                    { 12, "delete", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Delete exercises", "training", "training:exercises:delete", "exercises" },
                    { 13, "suggest", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Suggest exercises", "training", "training:exercises:suggest", "exercises" },
                    { 14, "create", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Create equipments", "training", "training:equipments:create", "equipments" },
                    { 15, "read", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Read equipments", "training", "training:equipments:read", "equipments" },
                    { 16, "update", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Update equipments", "training", "training:equipments:update", "equipments" },
                    { 17, "delete", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Delete equipments", "training", "training:equipments:delete", "equipments" },
                    { 18, "create", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Create workout sessions", "training", "training:workouts:create", "workouts" },
                    { 19, "read", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Read workout sessions", "training", "training:workouts:read", "workouts" },
                    { 20, "complete", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Complete workout sessions", "training", "training:workouts:complete", "workouts" },
                    { 21, "read", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Read training dashboard", "training", "training:dashboard:read", "dashboard" },
                    { 22, "create_self", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Create workouts for self", "training", "training:workouts:create:self", "workouts" },
                    { 23, "create_trainer", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Create workouts as trainer for trainer-client links", "training", "training:workouts:create:trainer", "workouts" },
                    { 24, "create_gym_staff", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Create workouts as gym trainer staff", "training", "training:workouts:create:gym_staff", "workouts" },
                    { 25, "create", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Create muscles", "training", "training:muscles:create", "muscles" },
                    { 26, "read", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Read muscles", "training", "training:muscles:read", "muscles" },
                    { 27, "update", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Update muscles", "training", "training:muscles:update", "muscles" },
                    { 28, "delete", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Delete muscles", "training", "training:muscles:delete", "muscles" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 28);
        }
    }
}
