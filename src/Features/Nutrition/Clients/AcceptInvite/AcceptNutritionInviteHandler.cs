using ShapeUp.Features.Nutrition.Clients.InviteClient;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Clients.AcceptInvite;

/// <summary>The client's acceptance is the consent to share nutrition data with the nutritionist.</summary>
public class AcceptNutritionInviteHandler(
    IProfessionalClientInviteRepository inviteRepository,
    IProfessionalClientRelationshipRepository relationshipRepository)
{
    public async Task<Result<AcceptNutritionInviteResponse>> HandleAsync(
        AcceptNutritionInviteCommand command,
        int clientUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            return Result<AcceptNutritionInviteResponse>.Failure(CommonErrors.Validation("Token is required."));

        var invite = await inviteRepository.GetByTokenHashAsync(InviteNutritionClientHandler.ComputeHash(command.Token.Trim()), cancellationToken);
        if (invite is null || invite.RelationshipType != NutritionAccessPolicy.RelationshipType)
            return Result<AcceptNutritionInviteResponse>.Failure(CommonErrors.NotFound("Invite not found."));

        if (invite.Status != ProfessionalClientInviteStatus.Pending)
            return Result<AcceptNutritionInviteResponse>.Failure(CommonErrors.Conflict("Invite is no longer available."));

        var nowUtc = DateTime.UtcNow;
        if (invite.ExpiresAtUtc <= nowUtc)
            return Result<AcceptNutritionInviteResponse>.Failure(CommonErrors.Validation("Invite has expired."));

        if (invite.ProfessionalUserId == clientUserId)
            return Result<AcceptNutritionInviteResponse>.Failure(CommonErrors.Validation("You cannot accept your own invite."));

        // Single use under concurrency: only the caller whose conditional UPDATE (Status = Pending) matches a row proceeds.
        if (!await inviteRepository.TryAcceptAsync(invite.Id, clientUserId, nowUtc, cancellationToken))
            return Result<AcceptNutritionInviteResponse>.Failure(CommonErrors.Conflict("Invite is no longer available."));

        DateTime startedAt;
        try
        {
            var existing = await relationshipRepository.GetActiveAsync(
                invite.ProfessionalUserId, clientUserId, NutritionAccessPolicy.RelationshipType, cancellationToken);

            startedAt = existing?.StartedAt ?? nowUtc;
            if (existing is null)
            {
                var created = await relationshipRepository.CreateAsync(new ProfessionalClientRelationship
                {
                    ProfessionalUserId = invite.ProfessionalUserId,
                    ClientUserId = clientUserId,
                    RelationshipType = NutritionAccessPolicy.RelationshipType,
                    StartedAt = nowUtc
                }, cancellationToken);

                if (created.IsFailure)
                {
                    await inviteRepository.ReleaseAcceptedAsync(invite.Id, clientUserId, CancellationToken.None);
                    return Result<AcceptNutritionInviteResponse>.Failure(created.Error!);
                }
            }
        }
        catch
        {
            // The invite was consumed but the link was not created: give it back so the client can retry.
            await inviteRepository.ReleaseAcceptedAsync(invite.Id, clientUserId, CancellationToken.None);
            throw;
        }

        return Result<AcceptNutritionInviteResponse>.Success(new AcceptNutritionInviteResponse(invite.ProfessionalUserId, startedAt));
    }
}
