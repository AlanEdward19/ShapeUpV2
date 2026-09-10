using FluentValidation;
using System.Security.Cryptography;
using System.Text;
using ShapeUp.Features.Nutrition.Diary.AddDiaryEntry;
using ShapeUp.Features.Nutrition.MealPlans.Shared;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlans.ActivateMealPlan;

public class ActivateMealPlanHandler(
    IMealPlanRepository mealPlanRepository,
    IFoodRepository foodRepository,
    AddDiaryEntryHandler addDiaryEntryHandler,
    IValidator<ActivateMealPlanCommand> validator)
{
    public async Task<Result<ActivateMealPlanResponse>> HandleAsync(
        ActivateMealPlanCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<ActivateMealPlanResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var plan = await mealPlanRepository.GetByIdAsync(command.MealPlanId, cancellationToken);
        if (plan is null || plan.UserId != actorUserId)
            return Result<ActivateMealPlanResponse>.Failure(NutritionErrors.MealPlanNotFound(command.MealPlanId));

        var existingPlans = await mealPlanRepository.GetByUserAsync(actorUserId, cancellationToken);
        foreach (var existing in existingPlans.Where(p => p.IsActive && p.Id != plan.Id))
        {
            existing.IsActive = false;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            await mealPlanRepository.UpdateAsync(existing, cancellationToken);
        }

        plan.IsActive = true;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await mealPlanRepository.UpdateAsync(plan, cancellationToken);

        var unavailableItems = new List<UnavailableMealPlanItemDto>();
        Diary.Shared.ViewModels.DiaryDayResponse? diaryDay = null;

        for (var index = 0; index < plan.Items.Count; index++)
        {
            var item = plan.Items[index];
            var food = await foodRepository.GetByIdIncludingDeletedAsync(item.FoodId, cancellationToken);
            if (food is null)
            {
                unavailableItems.Add(new UnavailableMealPlanItemDto(
                    item.MealSlot,
                    item.FoodId,
                    item.QuantityGramsOrMl,
                    "Food not found."));
                continue;
            }

            if (food.IsDeleted)
            {
                unavailableItems.Add(new UnavailableMealPlanItemDto(
                    item.MealSlot,
                    item.FoodId,
                    item.QuantityGramsOrMl,
                    "Food is no longer available."));
                continue;
            }

            var entryId = BuildPlanEntryId(plan.Id, index);
            var addResult = await addDiaryEntryHandler.HandleAsync(
                new AddDiaryEntryCommand(entryId, command.Date, item.MealSlot, item.FoodId, item.QuantityGramsOrMl),
                actorUserId,
                cancellationToken);

            if (!addResult.IsSuccess)
                return Result<ActivateMealPlanResponse>.Failure(addResult.Error!);

            diaryDay = addResult.Value;
        }

        diaryDay ??= new Diary.Shared.ViewModels.DiaryDayResponse(
            command.Date,
            [],
            new Diary.Shared.ViewModels.MacroTotalsDto(0, 0, 0, 0));

        return Result<ActivateMealPlanResponse>.Success(new ActivateMealPlanResponse(
            MealPlanMapper.ToResponse(plan),
            diaryDay,
            unavailableItems.ToArray()));
    }

    private static string BuildPlanEntryId(string planId, int index)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{planId}:{index}"));
        return Convert.ToHexString(hash)[..24].ToLowerInvariant();
    }
}
