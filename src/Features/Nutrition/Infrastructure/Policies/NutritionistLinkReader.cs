using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Abstractions;

namespace ShapeUp.Features.Nutrition.Infrastructure.Policies;

public class NutritionistLinkReader(
    IProfessionalClientRelationshipRepository relationshipRepository,
    IUserRepository userRepository) : INutritionistLinkReader
{
    public async Task<NutritionistLinkDto?> GetAsync(int userId, CancellationToken cancellationToken)
    {
        var relationships = await relationshipRepository.ListActiveByClientAsync(
            userId, NutritionAccessPolicy.RelationshipType, cancellationToken);
        var first = relationships.OrderBy(r => r.StartedAt).FirstOrDefault();
        if (first is null)
            return null;

        var user = await userRepository.GetByIdAsync(first.ProfessionalUserId, cancellationToken);
        return new NutritionistLinkDto(first.ProfessionalUserId, user?.DisplayName);
    }
}
