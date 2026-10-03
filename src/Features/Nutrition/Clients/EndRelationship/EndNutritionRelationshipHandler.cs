using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Clients.EndRelationship;

/// <summary>Either side can end the link; the client revoking it also revokes the nutritionist's access to their data.</summary>
public class EndNutritionRelationshipHandler(IProfessionalClientRelationshipRepository relationshipRepository)
{
    public async Task<Result> HandleAsync(
        int actorUserId,
        int professionalUserId,
        int clientUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId != professionalUserId && actorUserId != clientUserId)
            return Result.Failure(CommonErrors.Forbidden("You are not part of this relationship."));

        var ended = await relationshipRepository.EndActiveAsync(
            professionalUserId, clientUserId, NutritionAccessPolicy.RelationshipType, DateTime.UtcNow, cancellationToken);

        return ended
            ? Result.Success()
            : Result.Failure(CommonErrors.NotFound("No active nutrition relationship found."));
    }
}
