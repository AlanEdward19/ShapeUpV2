using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

public interface IFoodOverrideRepository
{
    Task<FoodOverrideDocument?> GetActiveForUserAsync(string foodId, int userId, CancellationToken cancellationToken);
    Task CreateAsync(FoodOverrideDocument overrideDocument, CancellationToken cancellationToken);
    Task SetActiveAsync(string overrideId, int userId, bool isActive, CancellationToken cancellationToken);
}
