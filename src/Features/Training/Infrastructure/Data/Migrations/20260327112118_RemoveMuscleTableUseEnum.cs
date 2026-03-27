using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.Training.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMuscleTableUseEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseMuscleProfiles_Muscles_MuscleId",
                table: "ExerciseMuscleProfiles");

            migrationBuilder.DropTable(
                name: "Muscles");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseMuscleProfiles_ExerciseId_MuscleId",
                table: "ExerciseMuscleProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseMuscleProfiles_MuscleId",
                table: "ExerciseMuscleProfiles");

            migrationBuilder.DropColumn(
                name: "MuscleId",
                table: "ExerciseMuscleProfiles");

            migrationBuilder.AddColumn<long>(
                name: "MuscleGroup",
                table: "ExerciseMuscleProfiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseMuscleProfiles_ExerciseId_MuscleGroup",
                table: "ExerciseMuscleProfiles",
                columns: new[] { "ExerciseId", "MuscleGroup" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExerciseMuscleProfiles_ExerciseId_MuscleGroup",
                table: "ExerciseMuscleProfiles");

            migrationBuilder.DropColumn(
                name: "MuscleGroup",
                table: "ExerciseMuscleProfiles");

            migrationBuilder.AddColumn<int>(
                name: "MuscleId",
                table: "ExerciseMuscleProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Muscles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NamePt = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Muscles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseMuscleProfiles_ExerciseId_MuscleId",
                table: "ExerciseMuscleProfiles",
                columns: new[] { "ExerciseId", "MuscleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseMuscleProfiles_MuscleId",
                table: "ExerciseMuscleProfiles",
                column: "MuscleId");

            migrationBuilder.CreateIndex(
                name: "IX_Muscles_Name",
                table: "Muscles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Muscles_NamePt",
                table: "Muscles",
                column: "NamePt",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseMuscleProfiles_Muscles_MuscleId",
                table: "ExerciseMuscleProfiles",
                column: "MuscleId",
                principalTable: "Muscles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
