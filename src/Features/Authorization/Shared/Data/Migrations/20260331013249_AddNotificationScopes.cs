using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShapeUp.Features.Authorization.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Scopes",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Domain", "Name", "Subdomain" },
                values: new object[,]
                {
                    { 73, "send_html", new DateTime(2026, 3, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Send notification emails with HTML body", "notifications", "notifications:emails:send_html", "emails" },
                    { 74, "send_template", new DateTime(2026, 3, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Send notification emails with template id", "notifications", "notifications:emails:send_template", "emails" }
                });

            migrationBuilder.InsertData(
                table: "GroupScopes",
                columns: new[] { "GroupId", "ScopeId", "AssignedAt" },
                values: new object[,]
                {
                    { 9999, 73, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 74, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "GroupScopes",
                keyColumns: new[] { "GroupId", "ScopeId" },
                keyValues: new object[] { 9999, 73 });

            migrationBuilder.DeleteData(
                table: "GroupScopes",
                keyColumns: new[] { "GroupId", "ScopeId" },
                keyValues: new object[] { 9999, 74 });

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "Scopes",
                keyColumn: "Id",
                keyValue: 74);
        }
    }
}
