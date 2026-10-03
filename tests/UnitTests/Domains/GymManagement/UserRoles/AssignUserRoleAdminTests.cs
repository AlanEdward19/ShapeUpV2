using Moq;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.UserRoles.AssignUserRole;

namespace UnitTests.Domains.GymManagement.UserRoles;

public class AssignUserRoleAdminTests
{
    private readonly Mock<IUserPlatformRoleRepository> _roles = new();
    private readonly Mock<IPlatformTierRepository> _tiers = new();

    [Fact]
    public async Task HandleAsync_AdminRole_IsAssigned()
    {
        var handler = new AssignUserRoleHandler(_roles.Object, _tiers.Object, new AssignUserRoleValidator());

        var result = await handler.HandleAsync(new AssignUserRoleCommand(7, PlatformRoleType.Admin, null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Admin", result.Value!.Role);
        _roles.Verify(r => r.AddAsync(It.Is<UserPlatformRole>(x => x.UserId == 7 && x.Role == PlatformRoleType.Admin), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ClientRole_StillCannotBeAssigned()
    {
        var handler = new AssignUserRoleHandler(_roles.Object, _tiers.Object, new AssignUserRoleValidator());

        var result = await handler.HandleAsync(new AssignUserRoleCommand(7, PlatformRoleType.Client, null), default);

        Assert.True(result.IsFailure);
    }
}
