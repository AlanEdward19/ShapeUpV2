using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShapeUp.Features.Authorization.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScopesReadScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Scopes",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Domain", "Name", "Subdomain" },
                values: new object[,]
                {
                    { 29, "read", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read groups", "groups", "groups:management:read", "management" },
                    { 30, "read", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read scopes", "scopes", "scopes:management:read", "management" },
                    { 31, "read", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read user profile", "users", "users:profile:read", "profile" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 31);
        }
    }
}
