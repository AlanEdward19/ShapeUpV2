using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.GymManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGrantedByCredentialToUserPlatformRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GrantedByCredentialId",
                table: "UserPlatformRoles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPlatformRoles_GrantedByCredentialId",
                table: "UserPlatformRoles",
                column: "GrantedByCredentialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserPlatformRoles_GrantedByCredentialId",
                table: "UserPlatformRoles");

            migrationBuilder.DropColumn(
                name: "GrantedByCredentialId",
                table: "UserPlatformRoles");
        }
    }
}
