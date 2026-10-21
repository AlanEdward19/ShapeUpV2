using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Authorization.UserManagement.SetUserActive;

namespace UnitTests.Domains.Authorization.UserManagement;

public class SetUserActiveHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly SetUserActiveHandler _handler;

    public SetUserActiveHandlerTests() => _handler = new SetUserActiveHandler(_users.Object);

    [Fact]
    public async Task HandleAsync_DeactivateOtherUser_UpdatesAndReturnsInactive()
    {
        _users.Setup(x => x.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Id = 4,
            FirebaseUid = "fb",
            Email = "ada@example.com",
            DisplayName = "Ada",
            IsActive = true
        });
        _users.Setup(x => x.SetActiveAsync(4, false, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.HandleAsync(new SetUserActiveCommand(4, false, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsActive);
        _users.Verify(x => x.SetActiveAsync(4, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DeactivateSelf_ReturnsConflict()
    {
        var result = await _handler.HandleAsync(new SetUserActiveCommand(7, false, 7), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.Error!.StatusCode);
        _users.Verify(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_MissingUser_ReturnsNotFound()
    {
        _users.Setup(x => x.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(new SetUserActiveCommand(9, true, 1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.Error!.StatusCode);
    }
}
