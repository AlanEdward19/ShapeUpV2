using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Training.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ExerciseType = table.Column<int>(type: "int", nullable: false),
                    MuscleGroups = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    EquipmentIds = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    EquipmentOther = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReferenceUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    RequestedByRole = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedExerciseId = table.Column<int>(type: "int", nullable: true),
                    ApprovedExerciseName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(280)", maxLength: 280, nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseRequests", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "ExerciseRequestId",
                table: "Exercises",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ExerciseRequestId",
                table: "Exercises",
                column: "ExerciseRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseRequests_ApprovedExerciseId",
                table: "ExerciseRequests",
                column: "ApprovedExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseRequests_RequestedByUserId_Id",
                table: "ExerciseRequests",
                columns: new[] { "RequestedByUserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseRequests_Status_Id",
                table: "ExerciseRequests",
                columns: new[] { "Status", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_ExerciseRequests_ExerciseRequestId",
                table: "Exercises",
                column: "ExerciseRequestId",
                principalTable: "ExerciseRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseRequests_Exercises_ApprovedExerciseId",
                table: "ExerciseRequests",
                column: "ApprovedExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_ExerciseRequests_ExerciseRequestId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseRequests_Exercises_ApprovedExerciseId",
                table: "ExerciseRequests");

            migrationBuilder.DropTable(
                name: "ExerciseRequests");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_ExerciseRequestId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ExerciseRequestId",
                table: "Exercises");
        }
    }
}
