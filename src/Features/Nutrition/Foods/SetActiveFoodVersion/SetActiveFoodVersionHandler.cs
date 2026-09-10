using FluentValidation;
using ShapeUp.Features.Nutrition.Foods.Shared;
using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Foods.SetActiveFoodVersion;

public class SetActiveFoodVersionHandler(
    IFoodRepository foodRepository,
    IFoodOverrideRepository foodOverrideRepository,
    IValidator<SetActiveFoodVersionCommand> validator)
{
    public async Task<Result<FoodResponse>> HandleAsync(
        string foodId,
        SetActiveFoodVersionCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<FoodResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var food = await foodRepository.GetByIdAsync(foodId, cancellationToken);
        if (food is null)
            return Result<FoodResponse>.Failure(NutritionErrors.FoodNotFound(foodId));

        var userOverride = await foodOverrideRepository.GetForUserAsync(foodId, actorUserId, cancellationToken);

        if (command.UsePersonalOverride)
        {
            if (userOverride is null)
                return Result<FoodResponse>.Failure(NutritionErrors.FoodOverrideNotFound(foodId));

            if (!userOverride.IsActive)
            {
                await foodOverrideRepository.SetActiveAsync(userOverride.Id, actorUserId, true, cancellationToken);
                userOverride.IsActive = true;
            }

            return Result<FoodResponse>.Success(FoodVersionResolver.Resolve(food, userOverride));
        }

        if (userOverride is not null && userOverride.IsActive)
        {
            await foodOverrideRepository.SetActiveAsync(userOverride.Id, actorUserId, false, cancellationToken);
            userOverride.IsActive = false;
        }

        return Result<FoodResponse>.Success(FoodVersionResolver.Resolve(food, null));
    }
}
