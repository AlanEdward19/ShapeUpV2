using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Authorization.UserManagement.GetUser;

namespace UnitTests.Domains.Authorization.UserManagement;

public class GetUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly GetUserHandler _handler;

    public GetUserHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _handler = new GetUserHandler(_mockUserRepository.Object);
    }

    public static IEnumerable<object[]> ExistingUserCases =>
        new List<object[]>
        {
            new object[] { 1, "firebase-123", "user1@example.com", "John Doe" },
            new object[] { 2, "firebase-456", "user2@example.com", "Jane Smith" },
            new object[] { 3, "firebase-789", "user3@example.com", "Bob Johnson" },
        };

    [Theory]
    [MemberData(nameof(ExistingUserCases))]
    public async Task HandleAsync_ExistingUser_ReturnsUser(
        int userId, string firebaseUid, string email, string displayName)
    {
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

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync(existingUser);

        var result = await _handler.HandleAsync(query, cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(email, result.Value.Email);
        Assert.Equal(displayName, result.Value.DisplayName);

        _mockUserRepository.Verify(x => x.GetByIdAsync(userId, cancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(999)]
    public async Task HandleAsync_UserNotFound_ReturnsFailure(int userId)
    {
        var query = new GetUserQuery(userId);
        var cancellationToken = CancellationToken.None;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(query, cancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(404, result.Error!.StatusCode);

        _mockUserRepository.Verify(x => x.GetByIdAsync(userId, cancellationToken), Times.Once);
    }
}
