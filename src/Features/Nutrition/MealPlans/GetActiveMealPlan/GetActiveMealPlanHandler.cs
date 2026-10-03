using ShapeUp.Features.Nutrition.MealPlans.Shared;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlans.GetActiveMealPlan;

public class GetActiveMealPlanHandler(IMealPlanRepository mealPlanRepository, INutritionAccessPolicy accessPolicy)
{
    public async Task<Result<MealPlanResponse>> HandleAsync(int actorUserId, int targetUserId, CancellationToken cancellationToken)
    {
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, targetUserId, cancellationToken))
            return Result<MealPlanResponse>.Failure(CommonErrors.Forbidden("You are not allowed to read this user's meal plans."));

        var plan = await mealPlanRepository.GetActiveAsync(targetUserId, cancellationToken);
        return plan is null
            ? Result<MealPlanResponse>.Failure(CommonErrors.NotFound("No active meal plan."))
            : Result<MealPlanResponse>.Success(MealPlanMapper.ToResponse(plan));
    }
}
