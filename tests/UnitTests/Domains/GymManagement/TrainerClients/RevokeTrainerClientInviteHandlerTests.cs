using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.TrainerClients.RevokeTrainerClientInvite;

namespace UnitTests.Domains.GymManagement.TrainerClients;

public class RevokeTrainerClientInviteHandlerTests
{
    private readonly Mock<ITrainerClientInviteRepository> _repository = new();

    private RevokeTrainerClientInviteHandler Handler() => new(_repository.Object);

    private static TrainerClientInvite Invite() => new()
    {
        Id = 9,
        TrainerId = 7,
        InviteeEmail = "a@x.com",
        Status = TrainerClientInviteStatus.Invited,
        ExpiresAtUtc = DateTime.UtcNow.AddHours(5)
    };

    [Fact]
    public async Task OwnInvite_IsRevokedThroughTheAtomicUpdate()
    {
        _repository.Setup(r => r.GetByIdAsync(9, default)).ReturnsAsync(Invite());
        _repository.Setup(r => r.TryRevokeAsync(9, 7, It.IsAny<DateTime>(), default)).ReturnsAsync(true);

        var result = await Handler().HandleAsync(7, 9, default);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.TryRevokeAsync(9, 7, It.IsAny<DateTime>(), default), Times.Once);
    }

    [Fact]
    public async Task MissingInviteOrAnotherTrainersInvite_Returns404AndRevokesNothing()
    {
        _repository.Setup(r => r.GetByIdAsync(9, default)).ReturnsAsync(Invite());

        Assert.Equal(404, (await Handler().HandleAsync(8, 9, default)).Error!.StatusCode);
        Assert.Equal(404, (await Handler().HandleAsync(7, 99, default)).Error!.StatusCode);
        _repository.Verify(r => r.TryRevokeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), default), Times.Never);
    }

    [Fact]
    public async Task WhenTheInviteIsNotWaitingAnymore_Returns409()
    {
        // Accepted a moment ago, expired, or already revoked: the conditional update matches no row.
        _repository.Setup(r => r.GetByIdAsync(9, default)).ReturnsAsync(Invite());
        _repository.Setup(r => r.TryRevokeAsync(9, 7, It.IsAny<DateTime>(), default)).ReturnsAsync(false);

        var result = await Handler().HandleAsync(7, 9, default);

        Assert.Equal(409, result.Error!.StatusCode);
    }
}
