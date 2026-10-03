using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Abstractions;

namespace ShapeUp.Features.Nutrition.Infrastructure.Policies;

public class NutritionAccessPolicy(
    IProfessionalCapabilityService capabilityService,
    IProfessionalClientRelationshipRepository relationshipRepository) : INutritionAccessPolicy
{
    public const string RelationshipType = "Nutrition";

    public async Task<bool> CanManageNutritionForAsync(int actorUserId, int targetUserId, CancellationToken cancellationToken)
    {
        if (actorUserId == targetUserId)
            return true;

        var capabilities = await capabilityService.GetAsync(actorUserId, cancellationToken);
        if (!capabilities.Nutrition)
            return false;

        var relationship = await relationshipRepository.GetActiveAsync(actorUserId, targetUserId, RelationshipType, cancellationToken);
        return relationship is not null;
    }
}
