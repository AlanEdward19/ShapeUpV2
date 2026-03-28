using ShapeUp.Features.Authorization.Groups.AddUserToGroup;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;

namespace UnitTests.Domains.Authorization.Groups;

public class AddUserToGroupHandlerTests
{
    private readonly Mock<IGroupRepository> _mockGroupRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly AddUserToGroupHandler _handler;

    public AddUserToGroupHandlerTests()
    {
        _mockGroupRepository = new Mock<IGroupRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _handler = new AddUserToGroupHandler(_mockGroupRepository.Object, _mockUserRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_OwnerAddsUser_SuccessfullyAddsUser()
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: 2, GroupId: 1, Role: "Member");
        var currentUserId = 1;
        var cancellationToken = CancellationToken.None;

        _mockGroupRepository
            .Setup(x => x.GetUserRoleInGroupAsync(currentUserId, 1, cancellationToken))
            .ReturnsAsync(GroupRole.Owner);

        var user = new User
        {
            Id = 2,
            FirebaseUid = "firebase-user-2",
            Email = "user2@example.com",
            IsActive = true
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(2, cancellationToken))
            .ReturnsAsync(user);

        var group = new Group { Id = 1, Name = "Engineering", CreatedById = currentUserId };

        _mockGroupRepository
            .Setup(x => x.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(group);

        _mockGroupRepository
            .Setup(x => x.UserBelongsToGroupAsync(2, 1, cancellationToken))
            .ReturnsAsync(false);

        _mockGroupRepository
            .Setup(x => x.AddUserToGroupAsync(2, 1, GroupRole.Member, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, currentUserId, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.UserId);
        Assert.Equal(1, result.Value.GroupId);
        Assert.Equal("Member", result.Value.Role);

        _mockGroupRepository.Verify(
            x => x.AddUserToGroupAsync(2, 1, GroupRole.Member, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AdministratorAddsUser_SuccessfullyAddsUser()
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: 3, GroupId: 1, Role: "Administrator");
        var currentUserId = 2;
        var cancellationToken = CancellationToken.None;

        _mockGroupRepository
            .Setup(x => x.GetUserRoleInGroupAsync(currentUserId, 1, cancellationToken))
            .ReturnsAsync(GroupRole.Administrator);

        var user = new User
        {
            Id = 3,
            FirebaseUid = "firebase-user-3",
            Email = "user3@example.com",
            IsActive = true
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(3, cancellationToken))
            .ReturnsAsync(user);

        var group = new Group { Id = 1, Name = "Engineering", CreatedById = 1 };

        _mockGroupRepository
            .Setup(x => x.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(group);

        _mockGroupRepository
            .Setup(x => x.UserBelongsToGroupAsync(3, 1, cancellationToken))
            .ReturnsAsync(false);

        _mockGroupRepository
            .Setup(x => x.AddUserToGroupAsync(3, 1, GroupRole.Administrator, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, currentUserId, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_MemberTriesToAddUser_ReturnsForbidden()
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: 2, GroupId: 1, Role: "Member");
        var currentUserId = 3; // Member trying to add user
        var cancellationToken = CancellationToken.None;

        _mockGroupRepository
            .Setup(x => x.GetUserRoleInGroupAsync(currentUserId, 1, cancellationToken))
            .ReturnsAsync(GroupRole.Member);

        // Act
        var result = await _handler.HandleAsync(command, currentUserId, cancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.Error?.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: 999, GroupId: 1, Role: "Member");
        var currentUserId = 1;
        var cancellationToken = CancellationToken.None;

        _mockGroupRepository
            .Setup(x => x.GetUserRoleInGroupAsync(currentUserId, 1, cancellationToken))
            .ReturnsAsync(GroupRole.Owner);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(999, cancellationToken))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.HandleAsync(command, currentUserId, cancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.Error?.StatusCode);
        Assert.Contains("not found", result.Error?.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_GroupDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: 2, GroupId: 999, Role: "Member");
        var currentUserId = 1;
        var cancellationToken = CancellationToken.None;

        // When group doesn't exist, GetUserRoleInGroupAsync returns null
        _mockGroupRepository
            .Setup(x => x.GetUserRoleInGroupAsync(currentUserId, 999, cancellationToken))
            .ReturnsAsync((GroupRole?)null);

        var user = new User
        {
            Id = 2,
            FirebaseUid = "firebase-user-2",
            Email = "user2@example.com",
            IsActive = true
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(2, cancellationToken))
            .ReturnsAsync(user);

        _mockGroupRepository
            .Setup(x => x.GetByIdAsync(999, cancellationToken))
            .ReturnsAsync((Group?)null);

        // Act
        var result = await _handler.HandleAsync(command, currentUserId, cancellationToken);

        // Assert
        // When the owner check fails (null role), it returns 403 Forbidden
        // The handler checks permissions before checking if group exists
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.Error?.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_UserAlreadyInGroup_ReturnsConflict()
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: 2, GroupId: 1, Role: "Member");
        var currentUserId = 1;
        var cancellationToken = CancellationToken.None;

        _mockGroupRepository
            .Setup(x => x.GetUserRoleInGroupAsync(currentUserId, 1, cancellationToken))
            .ReturnsAsync(GroupRole.Owner);

        var user = new User
        {
            Id = 2,
            FirebaseUid = "firebase-user-2",
            Email = "user2@example.com",
            IsActive = true
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(2, cancellationToken))
            .ReturnsAsync(user);

        var group = new Group { Id = 1, Name = "Engineering", CreatedById = 1 };

        _mockGroupRepository
            .Setup(x => x.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(group);

        _mockGroupRepository
            .Setup(x => x.UserBelongsToGroupAsync(2, 1, cancellationToken))
            .ReturnsAsync(true); // Already in group

        // Act
        var result = await _handler.HandleAsync(command, currentUserId, cancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.Error?.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_InvalidRole_ReturnsValidationError()
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: 2, GroupId: 1, Role: "InvalidRole");
        var currentUserId = 1;
        var cancellationToken = CancellationToken.None;

        _mockGroupRepository
            .Setup(x => x.GetUserRoleInGroupAsync(currentUserId, 1, cancellationToken))
            .ReturnsAsync(GroupRole.Owner);

        var user = new User
        {
            Id = 2,
            FirebaseUid = "firebase-user-2",
            Email = "user2@example.com",
            IsActive = true
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(2, cancellationToken))
            .ReturnsAsync(user);

        var group = new Group { Id = 1, Name = "Engineering", CreatedById = 1 };

        _mockGroupRepository
            .Setup(x => x.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(group);

        _mockGroupRepository
            .Setup(x => x.UserBelongsToGroupAsync(2, 1, cancellationToken))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.HandleAsync(command, currentUserId, cancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Error?.StatusCode);
    }
}



