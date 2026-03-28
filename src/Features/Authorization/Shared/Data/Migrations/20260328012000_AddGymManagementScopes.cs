using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShapeUp.Features.Authorization.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGymManagementScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Scopes",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Domain", "Name", "Subdomain" },
                values: new object[,]
                {
                    { 32, "read_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read gym clients as gym owner or staff", "gym", "gym:clients:read:gym_staff", "clients" },
                    { 33, "create_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Enroll gym clients as gym owner or staff", "gym", "gym:clients:create:gym_staff", "clients" },
                    { 34, "assign_trainer_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Assign gym client trainer as gym owner or staff", "gym", "gym:clients:assign_trainer:gym_staff", "clients" },
                    { 35, "read_gym_plan", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read gym plans as gym owner or staff", "gym", "gym:plans:read", "plans" },
                    { 36, "create_gym_plan", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Create gym plans as gym owner or staff", "gym", "gym:plans:create", "plans" },
                    { 37, "update_gym_plan", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Update gym plans as gym owner or staff", "gym", "gym:plans:update", "plans" },
                    { 38, "delete_gym_plan", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Delete gym plans as gym owner or staff", "gym", "gym:plans:delete", "plans" },
                    { 39, "read_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read gyms as gym owner or staff", "gym", "gym:gyms:read:gym_staff", "gyms" },
                    { 40, "create_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Create gyms as gym owner or staff", "gym", "gym:gyms:create:gym_staff", "gyms" },
                    { 41, "update_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Update gyms as gym owner or staff", "gym", "gym:gyms:update:gym_staff", "gyms" },
                    { 42, "delete_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Delete gyms as gym owner or staff", "gym", "gym:gyms:delete:gym_staff", "gyms" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Scopes", keyColumn: "Id", keyValue: 32);
            migrationBuilder.DeleteData(table: "Scopes", keyColumn: "Id", keyValue: 33);
            migrationBuilder.DeleteData(table: "Scopes", keyColumn: "Id", keyValue: 34);
            migrationBuilder.DeleteData(table: "Scopes", keyColumn: "Id", keyValue: 35);
            migrationBuilder.DeleteData(table: "Scopes", keyColumn: "Id", keyValue: 36);
            migrationBuilder.DeleteData(table: "Scopes", keyColumn: "Id", keyValue: 37);
            migrationBuilder.DeleteData(table: "Scopes", keyColumn: "Id", keyValue: 38);
            migrationBuilder.DeleteData(table: "Scopes", keyColumn: "Id", keyValue: 39);
            migrationBuilder.DeleteData(table: "Scopes", keyColumn: "Id", keyValue: 40);
            migrationBuilder.DeleteData(table: "Scopes", keyColumn: "Id", keyValue: 41);
            migrationBuilder.DeleteData(table: "Scopes", keyColumn: "Id", keyValue: 42);
        }
    }
}

