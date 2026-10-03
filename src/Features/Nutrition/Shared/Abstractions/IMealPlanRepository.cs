using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

public interface IMealPlanRepository
{
    Task CreateAsync(MealPlanDocument mealPlan, CancellationToken cancellationToken);
    Task<MealPlanDocument?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MealPlanDocument>> GetByUserAsync(int userId, CancellationToken cancellationToken);
    Task<MealPlanDocument?> GetActiveAsync(int userId, CancellationToken cancellationToken);
    Task UpdateAsync(MealPlanDocument mealPlan, CancellationToken cancellationToken);

    /// <summary>
    /// Makes <paramref name="planId"/> the user's only active plan, atomically (one Mongo transaction), so a failure never
    /// leaves two active plans or none. A null <paramref name="planId"/> deactivates every plan of the user.
    /// </summary>
    Task SetExclusiveActiveAsync(int userId, string? planId, DateTime nowUtc, CancellationToken cancellationToken);
}
