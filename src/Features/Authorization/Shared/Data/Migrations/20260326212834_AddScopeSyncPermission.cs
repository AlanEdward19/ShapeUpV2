using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShapeUp.Features.Authorization.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScopeSyncPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Scopes",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Domain", "Name", "Subdomain" },
                values: new object[,]
                {
                    { 7, "assign", new DateTime(2026, 3, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Assign scope", "scopes", "scopes:management:assign", "management" },
                    { 8, "sync", new DateTime(2026, 3, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Synchronize user scopes to Firebase claims", "scopes", "scopes:management:sync", "management" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
