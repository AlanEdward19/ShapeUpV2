using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Clients.ListNutritionists;

/// <summary>Client side: the nutritionists that currently have access to the user's nutrition data.</summary>
public class ListMyNutritionistsHandler(
    IProfessionalClientRelationshipRepository relationshipRepository,
    IUserRepository userRepository)
{
    public async Task<Result<NutritionistResponse[]>> HandleAsync(int clientUserId, CancellationToken cancellationToken)
    {
        var relationships = await relationshipRepository.ListActiveByClientAsync(
            clientUserId, NutritionAccessPolicy.RelationshipType, cancellationToken);

        var nutritionists = new List<NutritionistResponse>();
        foreach (var relationship in relationships)
        {
            var user = await userRepository.GetByIdAsync(relationship.ProfessionalUserId, cancellationToken);
            nutritionists.Add(new NutritionistResponse(relationship.ProfessionalUserId, user?.DisplayName, relationship.StartedAt));
        }

        return Result<NutritionistResponse[]>.Success(nutritionists.ToArray());
    }
}
