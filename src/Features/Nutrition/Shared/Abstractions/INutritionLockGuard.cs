using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

/// <summary>
/// Server-side counterpart of the client locks: a user linked to a nutritionist cannot change their own goal, nor
/// restructure a meal plan the nutritionist sent in full. Only the user acting on their own data is restricted; the
/// nutritionist (actor different from owner) is never blocked.
/// </summary>
public interface INutritionLockGuard
{
    Task<bool> IsLinkedToNutritionistAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Fails with <c>nutrition.goal.lockedByNutritionist</c> when the owner edits their own goal while linked.</summary>
    Task<Result> EnsureGoalEditableAsync(int actorUserId, int ownerUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Fails with <c>nutrition.mealPlan.lockedByNutritionist</c> when a linked owner creates or activates a plan while their
    /// active plan is nutritionist-prescribed and not macros-only. Re-activating the prescribed plan itself
    /// (<paramref name="activatingPlanId"/>) is allowed.
    /// </summary>
    Task<Result> EnsureMealPlanStructureEditableAsync(
        int actorUserId, int ownerUserId, string? activatingPlanId, CancellationToken cancellationToken);
}
