using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ShapeUp.Features.Authorization.Shared.Data.Migrations;

[DbContext(typeof(AuthorizationDbContext))]
[Migration("20261018160000_AddMobileExperience")]
public partial class AddMobileExperience : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(name: "FirstMobileSeenAtUtc", table: "Users", type: "datetime2", nullable: true);
        // Existing accounts are familiar: do not replay an introduction at rollout.
        migrationBuilder.Sql("UPDATE [Users] SET [FirstMobileSeenAtUtc] = SYSUTCDATETIME() WHERE [FirstMobileSeenAtUtc] IS NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "FirstMobileSeenAtUtc", table: "Users");
}
