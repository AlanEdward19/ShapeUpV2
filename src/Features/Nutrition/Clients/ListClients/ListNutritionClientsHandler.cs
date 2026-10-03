using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Clients.ListClients;

public class ListNutritionClientsHandler(
    IProfessionalCapabilityService capabilityService,
    IProfessionalClientRelationshipRepository relationshipRepository,
    IUserRepository userRepository)
{
    public async Task<Result<NutritionClientResponse[]>> HandleAsync(int nutritionistUserId, CancellationToken cancellationToken)
    {
        var capabilities = await capabilityService.GetAsync(nutritionistUserId, cancellationToken);
        if (!capabilities.Nutrition)
            return Result<NutritionClientResponse[]>.Failure(CommonErrors.Forbidden("Nutrition capability is required."));

        var relationships = await relationshipRepository.ListActiveByProfessionalAsync(
            nutritionistUserId, NutritionAccessPolicy.RelationshipType, cancellationToken);

        var clients = new List<NutritionClientResponse>();
        foreach (var relationship in relationships)
        {
            var user = await userRepository.GetByIdAsync(relationship.ClientUserId, cancellationToken);
            clients.Add(new NutritionClientResponse(relationship.ClientUserId, user?.DisplayName, relationship.StartedAt));
        }

        return Result<NutritionClientResponse[]>.Success(clients.ToArray());
    }
}
