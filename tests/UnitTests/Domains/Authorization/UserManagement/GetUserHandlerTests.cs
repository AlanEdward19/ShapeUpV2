using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;

namespace UnitTests.Domains.Authorization.UserManagement;

public class GetUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IScopeRepository> _mockScopeRepository;
    private readonly GetUserHandler _handler;

    public GetUserHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockScopeRepository = new Mock<IScopeRepository>();
        _handler = new GetUserHandler(_mockUserRepository.Object, _mockScopeRepository.Object);
    }

    // -------------------------------------------------------------------------
    // Existing user cases
    // -------------------------------------------------------------------------

    public static IEnumerable<object[]> ExistingUserCases =>
        new List<object[]>
        {
            new object[] { 1, "firebase-123", "user1@example.com", "John Doe" },
            new object[] { 2, "firebase-456", "user2@example.com", "Jane Smith" },
            new object[] { 3, "firebase-789", "user3@example.com", "Bob Johnson" },
        };

    [Theory]
    [MemberData(nameof(ExistingUserCases))]
    public async Task HandleAsync_ExistingUser_ReturnsUserWithScopes(
        int userId, string firebaseUid, string email, string displayName)
    {
        // Arrange
        var query = new GetUserQuery(userId);
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
            new Scope { Id = 1, Name = "users:profile:read",   Domain = "users", Subdomain = "profile", Action = "read" },
            new Scope { Id = 2, Name = "users:profile:update", Domain = "users", Subdomain = "profile", Action = "update" },
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync(existingUser);

        _mockScopeRepository
            .Setup(x => x.GetUserScopesAsync(userId, cancellationToken))
            .ReturnsAsync(scopes);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(email, result.Value.Email);
        Assert.Equal(displayName, result.Value.DisplayName);
        Assert.Equal(2, result.Value.Scopes.Length);

        _mockUserRepository.Verify(x => x.GetByIdAsync(userId, cancellationToken), Times.Once);
        _mockScopeRepository.Verify(x => x.GetUserScopesAsync(userId, cancellationToken), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Existing user without scopes
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(10, "firebase-noscopes", "noscopes@example.com")]
    [InlineData(11, "firebase-noscopes2", "noscopes2@example.com")]
    public async Task HandleAsync_ExistingUserWithoutScopes_ReturnsUserWithEmptyScopes(
        int userId, string firebaseUid, string email)
    {
        // Arrange
        var query = new GetUserQuery(userId);
        var cancellationToken = CancellationToken.None;

        var existingUser = new User
        {
            Id = userId,
            FirebaseUid = firebaseUid,
            Email = email,
            IsActive = true
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync(existingUser);

        _mockScopeRepository
            .Setup(x => x.GetUserScopesAsync(userId, cancellationToken))
            .ReturnsAsync(new List<Scope>());

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Scopes);

        _mockUserRepository.Verify(x => x.GetByIdAsync(userId, cancellationToken), Times.Once);
        _mockScopeRepository.Verify(x => x.GetUserScopesAsync(userId, cancellationToken), Times.Once);
    }

    // -------------------------------------------------------------------------
    // User not found (creation is the middleware's responsibility — not this handler)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(999)]
    public async Task HandleAsync_UserNotFound_ReturnsFailure(int userId)
    {
        // Arrange
        var query = new GetUserQuery(userId);
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(404, result.Error!.StatusCode);

        _mockUserRepository.Verify(x => x.GetByIdAsync(userId, cancellationToken), Times.Once);
        _mockScopeRepository.Verify(x => x.GetUserScopesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
