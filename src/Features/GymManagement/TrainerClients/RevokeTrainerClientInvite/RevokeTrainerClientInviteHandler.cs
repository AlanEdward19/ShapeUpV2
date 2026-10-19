using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.GymManagement.TrainerClients.RevokeTrainerClientInvite;

/// <summary>The trainer deactivates an invite that was not used yet, so its link can no longer be accepted.</summary>
public class RevokeTrainerClientInviteHandler(
    ITrainerClientInviteRepository repository,
    TimeProvider? timeProvider = null)
{
    public async Task<Result> HandleAsync(int trainerId, int inviteId, CancellationToken cancellationToken)
    {
        var invite = await repository.GetByIdAsync(inviteId, cancellationToken);
        if (invite is null || invite.TrainerId != trainerId)
            return Result.Failure(CommonErrors.NotFound("Invite not found."));

        // Atomic: only an invite still waiting (and within its deadline) is revoked. If the client accepts at the
        // same time, exactly one of the two wins and the other gets a 409.
        if (!await repository.TryRevokeAsync(inviteId, trainerId, (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime, cancellationToken))
            return Result.Failure(CommonErrors.Conflict("Invite is no longer pending."));

        return Result.Success();
    }
}
