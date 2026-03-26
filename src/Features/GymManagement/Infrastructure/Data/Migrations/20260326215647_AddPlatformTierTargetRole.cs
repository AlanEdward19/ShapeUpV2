using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShapeUp.Features.GymManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformTierTargetRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetRole",
                table: "PlatformTiers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetRole",
                table: "PlatformTiers");
        }
    }
}
