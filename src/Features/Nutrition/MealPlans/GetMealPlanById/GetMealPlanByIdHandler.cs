using ShapeUp.Features.Nutrition.MealPlans.Shared;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlans.GetMealPlanById;

public class GetMealPlanByIdHandler(IMealPlanRepository mealPlanRepository, INutritionAccessPolicy accessPolicy)
{
    public async Task<Result<MealPlanResponse>> HandleAsync(
        string mealPlanId, int actorUserId, int targetUserId, CancellationToken cancellationToken)
    {
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, targetUserId, cancellationToken))
            return Result<MealPlanResponse>.Failure(CommonErrors.Forbidden("You are not allowed to read this user's meal plans."));

        var plan = await mealPlanRepository.GetByIdAsync(mealPlanId, cancellationToken);
        if (plan is null || plan.UserId != targetUserId)
            return Result<MealPlanResponse>.Failure(NutritionErrors.MealPlanNotFound(mealPlanId));

        return Result<MealPlanResponse>.Success(MealPlanMapper.ToResponse(plan));
    }
}
