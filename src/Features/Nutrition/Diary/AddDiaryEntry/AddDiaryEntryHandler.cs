using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Diary.Shared;
using ShapeUp.Features.Nutrition.Diary.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Foods.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Diary.AddDiaryEntry;

public class AddDiaryEntryHandler(
    NutritionDbContext dbContext,
    IFoodRepository foodRepository,
    IFoodOverrideRepository foodOverrideRepository,
    IValidator<AddDiaryEntryCommand> validator)
{
    public async Task<Result<DiaryDayResponse>> HandleAsync(
        AddDiaryEntryCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<DiaryDayResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var food = await foodRepository.GetByIdAsync(command.FoodId, cancellationToken);
        if (food is null)
            return Result<DiaryDayResponse>.Failure(NutritionErrors.FoodNotFound(command.FoodId));

        var activeOverride = await foodOverrideRepository.GetActiveForUserAsync(command.FoodId, actorUserId, cancellationToken);
        var resolved = FoodVersionResolver.Resolve(food, activeOverride);
        var usesOverride = resolved.IsPersonalOverride;
        var macrosPer100 = new MacroValueObject
        {
            Kcal = resolved.MacrosPer100.Kcal,
            ProteinG = resolved.MacrosPer100.ProteinG,
            CarbG = resolved.MacrosPer100.CarbG,
            FatG = resolved.MacrosPer100.FatG
        };
        var computedMacros = MacroCalculator.CalculateFromPer100(macrosPer100, command.QuantityGramsOrMl);

        var day = await dbContext.DiaryDays
            .Include(d => d.Entries)
            .FirstOrDefaultAsync(d => d.UserId == actorUserId && d.Date == command.Date, cancellationToken);

        if (day is null)
        {
            day = new DiaryDay
            {
                UserId = actorUserId,
                Date = command.Date
            };
            dbContext.DiaryDays.Add(day);
        }

        var existingEntry = day.Entries.FirstOrDefault(e => e.Id == command.Id);
        if (existingEntry is null)
        {
            existingEntry = new DiaryEntry { Id = command.Id };
            day.Entries.Add(existingEntry);
        }

        existingEntry.MealSlot = NormalizeMealSlot(command.MealSlot);
        existingEntry.FoodId = command.FoodId;
        existingEntry.UsesOverride = usesOverride;
        existingEntry.QuantityGramsOrMl = command.QuantityGramsOrMl;
        existingEntry.ComputedMacros = computedMacros;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<DiaryDayResponse>.Success(DiaryMapper.ToResponse(day));
    }

    private static string NormalizeMealSlot(string mealSlot) =>
        mealSlot.ToLowerInvariant() switch
        {
            "breakfast" => "breakfast",
            "lunch" => "lunch",
            "dinner" => "dinner",
            _ => "snack"
        };
}
