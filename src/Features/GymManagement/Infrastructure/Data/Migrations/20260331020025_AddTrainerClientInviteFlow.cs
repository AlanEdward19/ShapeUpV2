using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.GymManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerClientInviteFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TrainerPlanId",
                table: "TrainerClients",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "TrainerClientInvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainerId = table.Column<int>(type: "int", nullable: false),
                    InviteeEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    AccessTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TrainerPlanId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedByUserId = table.Column<int>(type: "int", nullable: true),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerClientInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerClientInvites_TrainerPlans_TrainerPlanId",
                        column: x => x.TrainerPlanId,
                        principalTable: "TrainerPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainerClientInvites_AccessTokenHash",
                table: "TrainerClientInvites",
                column: "AccessTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainerClientInvites_TrainerId_InviteeEmail_Status",
                table: "TrainerClientInvites",
                columns: new[] { "TrainerId", "InviteeEmail", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainerClientInvites_TrainerPlanId",
                table: "TrainerClientInvites",
                column: "TrainerPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainerClientInvites");

            migrationBuilder.AlterColumn<int>(
                name: "TrainerPlanId",
                table: "TrainerClients",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
