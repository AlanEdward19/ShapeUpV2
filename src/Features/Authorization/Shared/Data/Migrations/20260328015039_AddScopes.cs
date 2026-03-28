using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShapeUp.Features.Authorization.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 24,
                column: "Name",
                value: "training:workouts:create");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 32,
                column: "Name",
                value: "gym:clients:read");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 33,
                column: "Name",
                value: "gym:clients:create");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 34,
                column: "Name",
                value: "gym:clients:assign_trainer");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 39,
                column: "Name",
                value: "gym:read");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 40,
                column: "Name",
                value: "gym:create");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 41,
                column: "Name",
                value: "gym:update");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 42,
                column: "Name",
                value: "gym:delete");

            migrationBuilder.InsertData(
                table: "Scopes",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Domain", "Name", "Subdomain" },
                values: new object[,]
                {
                    { 43, "read_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read gym staff as gym owner or staff", "gym", "gym:staff:read", "staff" },
                    { 44, "create_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Add gym staff as gym owner or staff", "gym", "gym:staff:create", "staff" },
                    { 45, "delete_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Remove gym staff as gym owner or staff", "gym", "gym:staff:delete", "staff" },
                    { 46, "read", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read platform tiers", "gym", "gym:platform_tiers:read", "platform_tiers" },
                    { 47, "create", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Create platform tiers", "gym", "gym:platform_tiers:create", "platform_tiers" },
                    { 48, "update", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Update platform tiers", "gym", "gym:platform_tiers:update", "platform_tiers" },
                    { 49, "delete", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Delete platform tiers", "gym", "gym:platform_tiers:delete", "platform_tiers" },
                    { 50, "read", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read trainer clients", "gym", "gym:trainer_clients:read", "trainer_clients" },
                    { 51, "create", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Create trainer clients", "gym", "gym:trainer_clients:create", "trainer_clients" },
                    { 52, "transfer", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Transfer trainer clients", "gym", "gym:trainer_clients:transfer", "trainer_clients" },
                    { 53, "read", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read trainer plans", "gym", "gym:trainer_plans:read", "trainer_plans" },
                    { 54, "create", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Create trainer plans", "gym", "gym:trainer_plans:create", "trainer_plans" },
                    { 55, "update", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Update trainer plans", "gym", "gym:trainer_plans:update", "trainer_plans" },
                    { 56, "delete", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Delete trainer plans", "gym", "gym:trainer_plans:delete", "trainer_plans" },
                    { 57, "read", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read user roles", "gym", "gym:user_roles:read", "user_roles" },
                    { 58, "assign", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Assign user roles", "gym", "gym:user_roles:assign", "user_roles" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 24,
                column: "Name",
                value: "training:workouts:create:gym_staff");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 32,
                column: "Name",
                value: "gym:clients:read:gym_staff");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 33,
                column: "Name",
                value: "gym:clients:create:gym_staff");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 34,
                column: "Name",
                value: "gym:clients:assign_trainer:gym_staff");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 39,
                column: "Name",
                value: "gym:gyms:read:gym_staff");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 40,
                column: "Name",
                value: "gym:gyms:create:gym_staff");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 41,
                column: "Name",
                value: "gym:gyms:update:gym_staff");

            migrationBuilder.UpdateData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 42,
                column: "Name",
                value: "gym:gyms:delete:gym_staff");
        }
    }
}
