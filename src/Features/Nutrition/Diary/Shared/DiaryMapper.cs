using ShapeUp.Features.Nutrition.Diary.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Diary.Shared;

internal static class DiaryMapper
{
    internal static DiaryDayResponse ToResponse(DiaryDay day) =>
        ToResponse(day.Date, day.Entries);

    internal static DiaryDayResponse ToResponse(DateOnly date, IReadOnlyList<DiaryEntry> entries)
    {
        var entryDtos = entries
            .Select(ToEntryDto)
            .ToArray();

        var meals = entryDtos
            .GroupBy(e => e.MealId is not null
                ? (Key: "id:" + e.MealId, Slot: e.MealSlot.ToLowerInvariant())
                : (Key: "slot:" + e.MealSlot.ToLowerInvariant() + "|" + (e.MealName ?? string.Empty), Slot: e.MealSlot.ToLowerInvariant()))
            .OrderBy(g => MealSlotOrder(g.Key.Slot))
            .Select(g => new DiaryMealGroupDto(
                g.First().MealSlot,
                g.ToArray(),
                g.First().MealName,
                g.First().MealId))
            .ToArray();

        var totals = MacroCalculator.Sum(entries.Select(e => e.ComputedMacros));

        return new DiaryDayResponse(date, meals, ToTotalsDto(totals));
    }

    internal static DiaryEntryDto ToEntryDto(DiaryEntry entry) =>
        new(
            entry.Id,
            entry.MealSlot,
            entry.FoodId,
            entry.UsesOverride,
            entry.QuantityGramsOrMl,
            ToTotalsDto(entry.ComputedMacros),
            entry.MealName,
            entry.MealId);

    internal static MacroTotalsDto ToTotalsDto(MacroValueObject macros) =>
        new(macros.Kcal, macros.ProteinG, macros.CarbG, macros.FatG);

    private static int MealSlotOrder(string mealSlot) =>
        mealSlot.ToLowerInvariant() switch
        {
            "breakfast" => 0,
            "lunch" => 1,
            "dinner" => 2,
            "snack" => 3,
            _ => 99
        };
}
