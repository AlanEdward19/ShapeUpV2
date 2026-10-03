using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClientInvites;

public class GetTrainerClientInvitesHandler(ITrainerClientInviteRepository repository)
{
    private const int MaxInvites = 100;

    public async Task<Result<IReadOnlyList<GetTrainerClientInviteResponse>>> HandleAsync(
        int trainerId,
        CancellationToken cancellationToken)
    {
        var invites = await repository.GetByTrainerAsync(trainerId, MaxInvites, cancellationToken);
        var nowUtc = DateTime.UtcNow;

        IReadOnlyList<GetTrainerClientInviteResponse> response = invites
            .Select(invite => new GetTrainerClientInviteResponse(
                invite.Id,
                invite.InviteeEmail,
                invite.Status == TrainerClientInviteStatus.Invited && invite.ExpiresAtUtc <= nowUtc
                    ? TrainerClientInviteStatus.Expired.ToString()
                    : invite.Status.ToString(),
                invite.CreatedAtUtc,
                invite.ExpiresAtUtc,
                invite.AcceptedAtUtc))
            .ToList();

        return Result<IReadOnlyList<GetTrainerClientInviteResponse>>.Success(response);
    }
}
