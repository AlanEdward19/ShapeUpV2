using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClientInvites;

namespace UnitTests.Domains.GymManagement.TrainerClients;

public class GetTrainerClientInvitesHandlerTests
{
    private readonly Mock<ITrainerClientInviteRepository> _repository = new();

    [Fact]
    public async Task HandleAsync_UnacceptedInvitePastDeadline_IsReportedAsExpired()
    {
        _repository
            .Setup(repo => repo.GetByTrainerAsync(7, 100, default))
            .ReturnsAsync(
            [
                new TrainerClientInvite { Id = 1, TrainerId = 7, InviteeEmail = "a@x.com", Status = TrainerClientInviteStatus.Invited, ExpiresAtUtc = DateTime.UtcNow.AddHours(-1) },
                new TrainerClientInvite { Id = 2, TrainerId = 7, InviteeEmail = "b@x.com", Status = TrainerClientInviteStatus.Invited, ExpiresAtUtc = DateTime.UtcNow.AddHours(5) },
                new TrainerClientInvite { Id = 3, TrainerId = 7, InviteeEmail = "c@x.com", Status = TrainerClientInviteStatus.Accepted, ExpiresAtUtc = DateTime.UtcNow.AddHours(-5) }
            ]);

        var result = await new GetTrainerClientInvitesHandler(_repository.Object).HandleAsync(7, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Expired", "Invited", "Accepted"], result.Value!.Select(invite => invite.Status));
    }
}
