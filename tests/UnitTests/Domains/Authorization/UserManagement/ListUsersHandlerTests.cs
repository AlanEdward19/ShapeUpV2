using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Authorization.UserManagement.ListUsers;
using ShapeUp.Shared.Pagination;

namespace UnitTests.Domains.Authorization.UserManagement;

public class ListUsersHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly ListUsersHandler _handler;

    public ListUsersHandlerTests() => _handler = new ListUsersHandler(_users.Object);

    [Fact]
    public async Task HandleAsync_ReturnsPageAndCursorWhenMoreRemain()
    {
        var rows = Enumerable.Range(1, 3).Select(id => new User
        {
            Id = id,
            FirebaseUid = $"fb-{id}",
            Email = $"user{id}@example.com",
            DisplayName = $"User {id}",
            IsActive = id != 2,
            CreatedAt = new DateTime(2026, 1, id, 0, 0, 0, DateTimeKind.Utc)
        }).ToList();

        _users.Setup(x => x.SearchKeysetAsync(null, null, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var result = await _handler.HandleAsync(new ListUsersQuery(null, 2, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Length);
        Assert.Equal(1, result.Value.Items[0].UserId);
        Assert.False(result.Value.Items[1].IsActive);
        Assert.Equal(KeysetCursorCodec.EncodeLong(2), result.Value.NextCursor);
    }

    [Fact]
    public async Task HandleAsync_InvalidCursor_ReturnsValidation()
    {
        var result = await _handler.HandleAsync(new ListUsersQuery("not-a-cursor", 20, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Error!.StatusCode);
        _users.Verify(x => x.SearchKeysetAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
