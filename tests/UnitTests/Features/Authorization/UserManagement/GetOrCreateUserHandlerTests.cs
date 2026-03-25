using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;

namespace UnitTests.Features.Authorization.UserManagement;

public class GetOrCreateUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IScopeRepository> _mockScopeRepository;
    private readonly GetOrCreateUserHandler _handler;

    public GetOrCreateUserHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockScopeRepository = new Mock<IScopeRepository>();
        _handler = new GetOrCreateUserHandler(_mockUserRepository.Object, _mockScopeRepository.Object);
    }

    public static IEnumerable<object[]> ExistingUserCases =>
        new List<object[]>
        {
            new object[] { "firebase-123", "user1@example.com", "John Doe", 1 },
            new object[] { "firebase-456", "user2@example.com", "Jane Smith", 2 },
            new object[] { "firebase-789", "user3@example.com", "Bob Johnson", 3 },
        };

    [Theory]
    [MemberData(nameof(ExistingUserCases))]
    public async Task HandleAsync_ExistingUser_ReturnsUserWithScopes(
        string firebaseUid, string email, string displayName, int userId)
    {
        // Arrange
        var command = new GetOrCreateUserCommand(firebaseUid, email, displayName);
        var cancellationToken = CancellationToken.None;

        var existingUser = new User
        {
            Id = userId,
            FirebaseUid = firebaseUid,
            Email = email,
            DisplayName = displayName,
            IsActive = true
        };

        var scopes = new List<Scope>
        {
            new Scope { Id = 1, Name = "users:read", Domain = "users", Subdomain = "", Action = "read" },
            new Scope { Id = 2, Name = "users:write", Domain = "users", Subdomain = "", Action = "write" }
        };

        _mockUserRepository
            .Setup(x => x.GetByFirebaseUidAsync(firebaseUid, cancellationToken))
            .ReturnsAsync(existingUser);

        _mockScopeRepository
            .Setup(x => x.GetUserScopesAsync(existingUser.Id, cancellationToken))
            .ReturnsAsync(scopes);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(email, result.Value.Email);
        Assert.Equal(displayName, result.Value.DisplayName);
        Assert.Equal(2, result.Value.Scopes.Length);

        _mockUserRepository.Verify(x => x.GetByFirebaseUidAsync(firebaseUid, cancellationToken), Times.Once);
        _mockScopeRepository.Verify(x => x.GetUserScopesAsync(userId, cancellationToken), Times.Once);
    }

    public static IEnumerable<object[]> NewUserCases =>
        new List<object[]>
        {
            new object[] { "firebase-new-1", "newuser1@example.com", "Alice Brown" },
            new object[] { "firebase-new-2", "newuser2@example.com", "Charlie Wilson" },
            new object[] { "firebase-new-3", "newuser3@example.com", "Diana Prince" },
        };

    [Theory]
    [MemberData(nameof(NewUserCases))]
    public async Task HandleAsync_NewUser_CreatesUserWithoutScopes(
        string firebaseUid, string email, string displayName)
    {
        // Arrange
        var command = new GetOrCreateUserCommand(firebaseUid, email, displayName);
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.GetByFirebaseUidAsync(firebaseUid, cancellationToken))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), cancellationToken))
            .Callback<User, CancellationToken>((user, _) => user.Id = 10)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(email, result.Value.Email);
        Assert.Equal(displayName, result.Value.DisplayName);
        Assert.Empty(result.Value.Scopes);

        _mockUserRepository.Verify(
            x => x.GetByFirebaseUidAsync(firebaseUid, cancellationToken), Times.Once);
        _mockUserRepository.Verify(
            x => x.AddAsync(It.Is<User>(u => u.FirebaseUid == firebaseUid && u.Email == email), cancellationToken),
            Times.Once);
    }

    public static IEnumerable<object[]> NewUserWithoutDisplayNameCases =>
        new List<object[]>
        {
            new object[] { "firebase-minimal-1", "minimal1@example.com" },
            new object[] { "firebase-minimal-2", "minimal2@example.com" },
            new object[] { "firebase-minimal-3", "minimal3@example.com" },
        };

    [Theory]
    [MemberData(nameof(NewUserWithoutDisplayNameCases))]
    public async Task HandleAsync_NewUserWithoutDisplayName_CreatesUserSuccessfully(
        string firebaseUid, string email)
    {
        // Arrange
        var command = new GetOrCreateUserCommand(firebaseUid, email, null);
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.GetByFirebaseUidAsync(firebaseUid, cancellationToken))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), cancellationToken))
            .Callback<User, CancellationToken>((user, _) => user.Id = 20)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(email, result.Value.Email);
        Assert.Null(result.Value.DisplayName);

        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), cancellationToken), Times.Once);
    }
}




