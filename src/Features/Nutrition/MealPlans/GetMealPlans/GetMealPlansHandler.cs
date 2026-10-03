using ShapeUp.Features.Nutrition.MealPlans.Shared;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlans.GetMealPlans;

/// <summary>Meal plans of <c>targetUserId</c> (newest first); the user reads their own, a linked nutritionist a client's.</summary>
public class GetMealPlansHandler(IMealPlanRepository mealPlanRepository, INutritionAccessPolicy accessPolicy)
{
    public async Task<Result<MealPlanResponse[]>> HandleAsync(int actorUserId, int targetUserId, CancellationToken cancellationToken)
    {
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, targetUserId, cancellationToken))
            return Result<MealPlanResponse[]>.Failure(CommonErrors.Forbidden("You are not allowed to read this user's meal plans."));

        var plans = await mealPlanRepository.GetByUserAsync(targetUserId, cancellationToken);
        return Result<MealPlanResponse[]>.Success(plans.Select(MealPlanMapper.ToResponse).ToArray());
    }
}
