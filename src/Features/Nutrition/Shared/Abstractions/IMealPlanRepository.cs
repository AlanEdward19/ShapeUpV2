using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

public interface IMealPlanRepository
{
    Task CreateAsync(MealPlanDocument mealPlan, CancellationToken cancellationToken);
    Task<MealPlanDocument?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MealPlanDocument>> GetByUserAsync(int userId, CancellationToken cancellationToken);
    Task<MealPlanDocument?> GetActiveAsync(int userId, CancellationToken cancellationToken);
    Task UpdateAsync(MealPlanDocument mealPlan, CancellationToken cancellationToken);
}
