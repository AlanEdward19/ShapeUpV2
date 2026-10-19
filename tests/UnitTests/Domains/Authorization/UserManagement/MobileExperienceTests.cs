using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Authorization.UserManagement.MobileExperience;

namespace UnitTests.Domains.Authorization.UserManagement;

public class MobileExperienceTests
{
    [Theory]
    [InlineData("android", true)]
    [InlineData("ios", true)]
    [InlineData("", false)]
    [InlineData("web", false)]
    [InlineData("Android", false)]
    public void PlatformHasExplicitBounds(string platform, bool valid) =>
        Assert.Equal(valid, new RegisterMobileExperienceValidator()
            .Validate(new RegisterMobileExperienceCommand(platform)).IsValid);

    [Theory]
    [InlineData("android", true)]
    [InlineData("ios", false)]
    public async Task FirstAndRepeatedVisitsKeepTheSameTimestamp(string platform, bool first)
    {
        var stamp = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(
            new User { Id = 7, FirebaseUid = "u", Email = "u@example.test", FirstMobileSeenAtUtc = stamp });
        repo.Setup(r => r.TryRegisterMobileExperienceAsync(7, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(first);
        var handler = new RegisterMobileExperienceHandler(repo.Object, new RegisterMobileExperienceValidator());
        var result = await handler.HandleAsync(new(platform), 7, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(first, result.Value!.FirstMobileVisit);
        Assert.Equal(stamp, result.Value.FirstSeenAtUtc);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingOrInactiveAccountIsForbidden(bool missing)
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(missing ? null :
            new User { Id = 7, FirebaseUid = "u", Email = "u@example.test", IsActive = false });
        var result = await new RegisterMobileExperienceHandler(repo.Object, new RegisterMobileExperienceValidator())
            .HandleAsync(new("ios"), 7, CancellationToken.None);
        Assert.Equal(403, result.Error!.StatusCode);
        repo.Verify(r => r.TryRegisterMobileExperienceAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public void MigrationMarksOnlyAccountsExistingAtRollout()
    {
        var migration = new ShapeUp.Features.Authorization.Shared.Data.Migrations.AddMobileExperience();
        Assert.Contains(migration.UpOperations, operation => operation is Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation { Name: "FirstMobileSeenAtUtc", IsNullable: true });
        Assert.Contains(migration.UpOperations, operation => operation is Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation sql && sql.Sql.Contains("UPDATE [Users]") && sql.Sql.Contains("WHERE [FirstMobileSeenAtUtc] IS NULL"));
    }

}
