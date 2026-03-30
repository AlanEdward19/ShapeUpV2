using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShapeUp.Features.Authorization.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultAdministratorsGroupWithAllScopes : Migration
    {
        private const int AdministratorsGroupId = 9999;
        private const int LastSeededScopeId = 68;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                IF NOT EXISTS (SELECT 1 FROM [Groups] WHERE [Id] = {AdministratorsGroupId})
                BEGIN
                    SET IDENTITY_INSERT [Groups] ON;
                    INSERT INTO [Groups] ([Id], [CreatedAt], [CreatedById], [Description], [Name], [UpdatedAt])
                    VALUES ({AdministratorsGroupId}, '2026-03-29T00:00:00.0000000Z', 0, N'Default administrators group with full system access.', N'Administrators', '2026-03-29T00:00:00.0000000Z');
                    SET IDENTITY_INSERT [Groups] OFF;
                END
                """);

            migrationBuilder.Sql($"""
                INSERT INTO [GroupScopes] ([GroupId], [ScopeId], [AssignedAt])
                SELECT {AdministratorsGroupId}, [s].[Id], '2026-03-29T00:00:00.0000000Z'
                FROM [Scopes] AS [s]
                LEFT JOIN [GroupScopes] AS [gs]
                    ON [gs].[GroupId] = {AdministratorsGroupId}
                    AND [gs].[ScopeId] = [s].[Id]
                WHERE [s].[Id] BETWEEN 1 AND {LastSeededScopeId}
                  AND [gs].[GroupId] IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE FROM [GroupScopes] WHERE [GroupId] = {AdministratorsGroupId};
                DELETE FROM [Groups] WHERE [Id] = {AdministratorsGroupId};
                """);
        }
    }
}
