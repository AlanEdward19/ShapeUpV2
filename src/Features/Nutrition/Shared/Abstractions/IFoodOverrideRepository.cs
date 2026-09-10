using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

public interface IFoodOverrideRepository
{
    Task<FoodOverrideDocument?> GetActiveForUserAsync(string foodId, int userId, CancellationToken cancellationToken);
    Task<FoodOverrideDocument?> GetForUserAsync(string foodId, int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FoodOverrideDocument>> GetActiveForUserByFoodIdsAsync(
        int userId,
        IEnumerable<string> foodIds,
        CancellationToken cancellationToken);
    Task CreateAsync(FoodOverrideDocument overrideDocument, CancellationToken cancellationToken);
    Task UpdateAsync(FoodOverrideDocument overrideDocument, CancellationToken cancellationToken);
    Task SetActiveAsync(string overrideId, int userId, bool isActive, CancellationToken cancellationToken);
    Task<FoodOverrideDocument?> GetByIdAsync(string overrideId, CancellationToken cancellationToken);
    Task DeleteAsync(string overrideId, CancellationToken cancellationToken);
}
