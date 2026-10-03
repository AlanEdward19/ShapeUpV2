using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;
using ShapeUp.Features.Nutrition.Diary.AddDiaryEntry;
using ShapeUp.Features.Nutrition.MealPlans.Shared;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlans.ActivateMealPlan;

/// <summary>
/// Activates a plan and fills the diary day from it, all or nothing: the diary entries are written in one SQL
/// transaction, the activation flip is one Mongo transaction, and the SQL transaction commits last. If the activation
/// fails the entries are rolled back; if the final commit fails the previous active plan is restored.
/// The entry ids are derived from plan and position, so repeating the call is idempotent.
/// </summary>
public class ActivateMealPlanHandler(
    IMealPlanRepository mealPlanRepository,
    IFoodRepository foodRepository,
    AddDiaryEntryHandler addDiaryEntryHandler,
    NutritionDbContext dbContext,
    INutritionAccessPolicy accessPolicy,
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

        var ownerUserId = command.TargetUserId ?? actorUserId;
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, ownerUserId, cancellationToken))
            return Result<ActivateMealPlanResponse>.Failure(CommonErrors.Forbidden("You are not allowed to activate a meal plan for this user."));

        var plan = await mealPlanRepository.GetByIdAsync(command.MealPlanId, cancellationToken);
        if (plan is null || plan.UserId != ownerUserId)
            return Result<ActivateMealPlanResponse>.Failure(NutritionErrors.MealPlanNotFound(command.MealPlanId));

        // Read-only pass first: which items can be applied and which are unavailable.
        var unavailableItems = new List<UnavailableMealPlanItemDto>();
        var toApply = new List<AddDiaryEntryCommand>();
        for (var index = 0; index < plan.Items.Count; index++)
        {
            var item = plan.Items[index];
            var food = await foodRepository.GetByIdIncludingDeletedAsync(item.FoodId, cancellationToken);
            if (food is null || food.IsDeleted)
            {
                unavailableItems.Add(new UnavailableMealPlanItemDto(
                    item.MealSlot,
                    item.FoodId,
                    item.QuantityGramsOrMl,
                    food is null ? "Food not found." : "Food is no longer available."));
                continue;
            }

            toApply.Add(new AddDiaryEntryCommand(
                BuildPlanEntryId(plan.Id, index), command.Date, item.MealSlot, item.FoodId, item.QuantityGramsOrMl));
        }

        var previousActive = await mealPlanRepository.GetActiveAsync(ownerUserId, cancellationToken);
        Diary.Shared.ViewModels.DiaryDayResponse? diaryDay = null;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        foreach (var entry in toApply)
        {
            var addResult = await addDiaryEntryHandler.HandleAsync(entry, ownerUserId, cancellationToken);
            if (!addResult.IsSuccess)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Result<ActivateMealPlanResponse>.Failure(addResult.Error!);
            }

            diaryDay = addResult.Value;
        }

        var nowUtc = DateTime.UtcNow;
        await mealPlanRepository.SetExclusiveActiveAsync(ownerUserId, plan.Id, nowUtc, cancellationToken);
        try
        {
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await mealPlanRepository.SetExclusiveActiveAsync(ownerUserId, previousActive?.Id, DateTime.UtcNow, CancellationToken.None);
            throw;
        }

        plan.IsActive = true;
        plan.UpdatedAtUtc = nowUtc;

        diaryDay ??= new Diary.Shared.ViewModels.DiaryDayResponse(
            command.Date,
            [],
            new Diary.Shared.ViewModels.MacroTotalsDto(0, 0, 0, 0));

        return Result<ActivateMealPlanResponse>.Success(new ActivateMealPlanResponse(
            MealPlanMapper.ToResponse(plan),
            diaryDay,
            unavailableItems.ToArray(),
            toApply.Count,
            plan.Items.Count));
    }

    private static string BuildPlanEntryId(string planId, int index)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{planId}:{index}"));
        return Convert.ToHexString(hash)[..24].ToLowerInvariant();
    }
}
