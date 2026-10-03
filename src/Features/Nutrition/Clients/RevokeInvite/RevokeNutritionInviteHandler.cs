using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Clients.RevokeInvite;

/// <summary>The nutritionist withdraws a pending invite, so its token can no longer be accepted.</summary>
public class RevokeNutritionInviteHandler(IProfessionalClientInviteRepository inviteRepository)
{
    public async Task<Result> HandleAsync(int inviteId, int nutritionistUserId, CancellationToken cancellationToken)
    {
        var invite = await inviteRepository.GetByIdAsync(inviteId, cancellationToken);
        if (invite is null
            || invite.ProfessionalUserId != nutritionistUserId
            || invite.RelationshipType != NutritionAccessPolicy.RelationshipType)
            return Result.Failure(CommonErrors.NotFound("Invite not found."));

        if (invite.Status != ProfessionalClientInviteStatus.Pending
            || !await inviteRepository.TryRevokeAsync(inviteId, nutritionistUserId, cancellationToken))
            return Result.Failure(CommonErrors.Conflict("Invite is no longer pending."));

        return Result.Success();
    }
}
