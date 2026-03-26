using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.GymManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxClients = table.Column<int>(type: "int", nullable: true),
                    MaxTrainers = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainerPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gyms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PlatformTierId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gyms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gyms_PlatformTiers_PlatformTierId",
                        column: x => x.PlatformTierId,
                        principalTable: "PlatformTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserPlatformRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    PlatformTierId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPlatformRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPlatformRoles_PlatformTiers_PlatformTierId",
                        column: x => x.PlatformTierId,
                        principalTable: "PlatformTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TrainerClients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainerId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    TrainerPlanId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerClients_TrainerPlans_TrainerPlanId",
                        column: x => x.TrainerPlanId,
                        principalTable: "TrainerPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GymPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GymId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymPlans_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GymStaff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GymId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HiredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymStaff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymStaff_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GymClients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GymId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GymPlanId = table.Column<int>(type: "int", nullable: false),
                    TrainerId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymClients_GymPlans_GymPlanId",
                        column: x => x.GymPlanId,
                        principalTable: "GymPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymClients_GymStaff_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "GymStaff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymClients_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymClients_GymId_UserId",
                table: "GymClients",
                columns: new[] { "GymId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymClients_GymPlanId",
                table: "GymClients",
                column: "GymPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_GymClients_TrainerId",
                table: "GymClients",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_GymPlans_GymId",
                table: "GymPlans",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_Gyms_OwnerId",
                table: "Gyms",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Gyms_PlatformTierId",
                table: "Gyms",
                column: "PlatformTierId");

            migrationBuilder.CreateIndex(
                name: "IX_GymStaff_GymId_UserId",
                table: "GymStaff",
                columns: new[] { "GymId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformTiers_Name",
                table: "PlatformTiers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainerClients_TrainerId_ClientId",
                table: "TrainerClients",
                columns: new[] { "TrainerId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainerClients_TrainerPlanId",
                table: "TrainerClients",
                column: "TrainerPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerPlans_TrainerId",
                table: "TrainerPlans",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPlatformRoles_PlatformTierId",
                table: "UserPlatformRoles",
                column: "PlatformTierId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPlatformRoles_UserId_Role",
                table: "UserPlatformRoles",
                columns: new[] { "UserId", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymClients");

            migrationBuilder.DropTable(
                name: "TrainerClients");

            migrationBuilder.DropTable(
                name: "UserPlatformRoles");

            migrationBuilder.DropTable(
                name: "GymPlans");

            migrationBuilder.DropTable(
                name: "GymStaff");

            migrationBuilder.DropTable(
                name: "TrainerPlans");

            migrationBuilder.DropTable(
                name: "Gyms");

            migrationBuilder.DropTable(
                name: "PlatformTiers");
        }
    }
}
