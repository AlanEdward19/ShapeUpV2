using ShapeUp.Features.Nutrition.MealPlans.Shared;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Infrastructure.Policies;

public class NutritionLockGuard(
    IProfessionalClientRelationshipRepository relationshipRepository,
    IMealPlanRepository mealPlanRepository) : INutritionLockGuard
{
    public async Task<bool> IsLinkedToNutritionistAsync(int userId, CancellationToken cancellationToken) =>
        (await relationshipRepository.ListActiveByClientAsync(
            userId, NutritionAccessPolicy.RelationshipType, cancellationToken)).Count > 0;

    public async Task<Result> EnsureGoalEditableAsync(int actorUserId, int ownerUserId, CancellationToken cancellationToken)
    {
        if (actorUserId != ownerUserId)
            return Result.Success();

        return await IsLinkedToNutritionistAsync(ownerUserId, cancellationToken)
            ? Result.Failure(NutritionErrors.GoalLockedByNutritionist())
            : Result.Success();
    }

    public async Task<Result> EnsureMealPlanStructureEditableAsync(
        int actorUserId, int ownerUserId, string? activatingPlanId, CancellationToken cancellationToken)
    {
        if (actorUserId != ownerUserId || !await IsLinkedToNutritionistAsync(ownerUserId, cancellationToken))
            return Result.Success();

        var active = await mealPlanRepository.GetActiveAsync(ownerUserId, cancellationToken);
        if (active is null || active.PrescribedByUserId is null || active.PrescribedByUserId == ownerUserId)
            return Result.Success();

        if (activatingPlanId is not null)
        {
            if (active.Id == activatingPlanId)
                return Result.Success();

            // Switching to another plan the nutritionist prescribed is the nutritionist's decision, not a restructure.
            var target = await mealPlanRepository.GetByIdAsync(activatingPlanId, cancellationToken);
            if (target?.PrescribedByUserId is { } prescriber && prescriber != ownerUserId)
                return Result.Success();
        }

        return MealPlanMapper.IsMacroOnly(active)
            ? Result.Success()
            : Result.Failure(NutritionErrors.MealPlanLockedByNutritionist());
    }
}
