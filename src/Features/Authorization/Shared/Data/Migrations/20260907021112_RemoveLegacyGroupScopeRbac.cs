using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShapeUp.Features.Authorization.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyGroupScopeRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupScopes");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "UserScopes");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Scopes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scopes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Domain = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subdomain = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scopes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_UserGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupScopes",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    ScopeId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupScopes", x => new { x.GroupId, x.ScopeId });
                    table.ForeignKey(
                        name: "FK_GroupScopes_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupScopes_Scopes_ScopeId",
                        column: x => x.ScopeId,
                        principalTable: "Scopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserScopes",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ScopeId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserScopes", x => new { x.UserId, x.ScopeId });
                    table.ForeignKey(
                        name: "FK_UserScopes_Scopes_ScopeId",
                        column: x => x.ScopeId,
                        principalTable: "Scopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserScopes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Groups",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "Description", "Name", "UpdatedAt" },
                values: new object[] { 9999, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), 0, "Default administrators group with full system access.", "Administrators", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Scopes",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Domain", "Name", "Subdomain" },
                values: new object[,]
                {
                    { 1, "create", new DateTime(2026, 3, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Create groups", "groups", "groups:management:create", "management" },
                    { 2, "delete", new DateTime(2026, 3, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Delete groups", "groups", "groups:management:delete", "management" },
                    { 3, "manage_members", new DateTime(2026, 3, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Manage group members", "groups", "groups:management:manage_members", "management" },
                    { 4, "manage_scopes", new DateTime(2026, 3, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Manage group scopes", "groups", "groups:management:manage_scopes", "management" },
                    { 5, "create", new DateTime(2026, 3, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Create scopes", "scopes", "scopes:management:create", "management" },
                    { 6, "read", new DateTime(2026, 3, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Read audit logs", "audit", "audit:logs:read", "logs" },
                    { 7, "assign", new DateTime(2026, 3, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Assign scope", "scopes", "scopes:management:assign", "management" },
                    { 8, "sync", new DateTime(2026, 3, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Synchronize user scopes to Firebase claims", "scopes", "scopes:management:sync", "management" },
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
                    { 24, "create_gym_staff", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Create workouts as gym trainer staff", "training", "training:workouts:create", "workouts" },
                    { 25, "create", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Create muscles", "training", "training:muscles:create", "muscles" },
                    { 26, "read", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Read muscles", "training", "training:muscles:read", "muscles" },
                    { 27, "update", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Update muscles", "training", "training:muscles:update", "muscles" },
                    { 28, "delete", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Delete muscles", "training", "training:muscles:delete", "muscles" },
                    { 29, "read", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read groups", "groups", "groups:management:read", "management" },
                    { 30, "read", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read scopes", "scopes", "scopes:management:read", "management" },
                    { 31, "read", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read user profile", "users", "users:profile:read", "profile" },
                    { 32, "read_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read gym clients as gym owner or staff", "gym", "gym:clients:read", "clients" },
                    { 33, "create_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Enroll gym clients as gym owner or staff", "gym", "gym:clients:create", "clients" },
                    { 34, "assign_trainer_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Assign gym client trainer as gym owner or staff", "gym", "gym:clients:assign_trainer", "clients" },
                    { 35, "read_gym_plan", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read gym plans as gym owner or staff", "gym", "gym:plans:read", "plans" },
                    { 36, "create_gym_plan", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Create gym plans as gym owner or staff", "gym", "gym:plans:create", "plans" },
                    { 37, "update_gym_plan", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Update gym plans as gym owner or staff", "gym", "gym:plans:update", "plans" },
                    { 38, "delete_gym_plan", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Delete gym plans as gym owner or staff", "gym", "gym:plans:delete", "plans" },
                    { 39, "read_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Read gyms as gym owner or staff", "gym", "gym:read", "gyms" },
                    { 40, "create_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Create gyms as gym owner or staff", "gym", "gym:create", "gyms" },
                    { 41, "update_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Update gyms as gym owner or staff", "gym", "gym:update", "gyms" },
                    { 42, "delete_gym_staff", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Delete gyms as gym owner or staff", "gym", "gym:delete", "gyms" },
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
                    { 58, "assign", new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), "Assign user roles", "gym", "gym:user_roles:assign", "user_roles" },
                    { 59, "create", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Create workout plans", "training", "training:workout-plans:create", "workout_plans" },
                    { 60, "read", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Read workout plans", "training", "training:workout-plans:read", "workout_plans" },
                    { 61, "copy", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Copy workout plans", "training", "training:workout-plans:copy", "workout_plans" },
                    { 62, "create", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Create workout templates", "training", "training:workout-templates:create", "workout_templates" },
                    { 63, "read", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Read workout templates", "training", "training:workout-templates:read", "workout_templates" },
                    { 64, "copy", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Copy workout templates", "training", "training:workout-templates:copy", "workout_templates" },
                    { 65, "assign", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Assign workout templates to users", "training", "training:workout-templates:assign", "workout_templates" },
                    { 66, "start", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Start workout executions", "training", "training:workouts:start", "workouts" },
                    { 67, "update", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Update workout execution state", "training", "training:workouts:update", "workouts" },
                    { 68, "finish", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Finish workout executions", "training", "training:workouts:finish", "workouts" },
                    { 69, "update", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Update workout plans", "training", "training:workout-plans:update", "workout_plans" },
                    { 70, "delete", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Delete workout plans", "training", "training:workout-plans:delete", "workout_plans" },
                    { 71, "update", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Update workout templates", "training", "training:workout-templates:update", "workout_templates" },
                    { 72, "delete", new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Delete workout templates", "training", "training:workout-templates:delete", "workout_templates" },
                    { 73, "send_html", new DateTime(2026, 3, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Send notification emails with HTML body", "notifications", "notifications:emails:send_html", "emails" },
                    { 74, "send_template", new DateTime(2026, 3, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Send notification emails with template id", "notifications", "notifications:emails:send_template", "emails" }
                });

            migrationBuilder.InsertData(
                table: "GroupScopes",
                columns: new[] { "GroupId", "ScopeId", "AssignedAt" },
                values: new object[,]
                {
                    { 9999, 1, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 2, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 3, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 4, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 5, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 6, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 7, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 8, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 9, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 10, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 11, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 12, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 13, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 14, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 15, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 16, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 17, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 18, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 19, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 20, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 21, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 22, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 23, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 24, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 25, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 26, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 27, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 28, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 29, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 30, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 31, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 32, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 33, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 34, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 35, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 36, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 37, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 38, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 39, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 40, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 41, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 42, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 43, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 44, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 45, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 46, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 47, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 48, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 49, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 50, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 51, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 52, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 53, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 54, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 55, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 56, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 57, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 58, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 59, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 60, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 61, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 62, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 63, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 64, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 65, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 66, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 67, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 68, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 69, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 70, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 71, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 72, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 73, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9999, 74, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupScopes_ScopeId",
                table: "GroupScopes",
                column: "ScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_Scopes_Domain_Subdomain_Action",
                table: "Scopes",
                columns: new[] { "Domain", "Subdomain", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_GroupId",
                table: "UserGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserScopes_ScopeId",
                table: "UserScopes",
                column: "ScopeId");
        }
    }
}
