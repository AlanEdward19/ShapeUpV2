using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShapeUp.Features.Authorization.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingWorkoutPlanTemplateUpdateDeleteScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Scopes",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Domain", "Name", "Subdomain" },
                values: new object[,]
                {
                    { 69, "update", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Update workout plans", "training", "training:workout-plans:update", "workout_plans" },
                    { 70, "delete", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Delete workout plans", "training", "training:workout-plans:delete", "workout_plans" },
                    { 71, "update", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Update workout templates", "training", "training:workout-templates:update", "workout_templates" },
                    { 72, "delete", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Delete workout templates", "training", "training:workout-templates:delete", "workout_templates" }
                });

            migrationBuilder.InsertData(
                table: "GroupScopes",
                columns: new[] { "GroupId", "ScopeId", "AssignedAt" },
                values: new object[,]
                {
                    { 9999, 69, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 70, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 71, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 72, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "GroupScopes",
                keyColumns: new[] { "GroupId", "ScopeId" },
                keyValues: new object[] { 9999, 69 });

            migrationBuilder.DeleteData(
                table: "GroupScopes",
                keyColumns: new[] { "GroupId", "ScopeId" },
                keyValues: new object[] { 9999, 70 });

            migrationBuilder.DeleteData(
                table: "GroupScopes",
                keyColumns: new[] { "GroupId", "ScopeId" },
                keyValues: new object[] { 9999, 71 });

            migrationBuilder.DeleteData(
                table: "GroupScopes",
                keyColumns: new[] { "GroupId", "ScopeId" },
                keyValues: new object[] { 9999, 72 });

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 72);
        }
    }
}
