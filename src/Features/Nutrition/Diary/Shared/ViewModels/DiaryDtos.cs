namespace ShapeUp.Features.Nutrition.Diary.Shared.ViewModels;

public record MacroTotalsDto(int Kcal, int ProteinG, int CarbG, int FatG);

public record DiaryEntryDto(
    string Id,
    string MealSlot,
    string FoodId,
    bool UsesOverride,
    decimal QuantityGramsOrMl,
    MacroTotalsDto ComputedMacros,
    string? MealName = null,
    string? MealId = null);

public record DiaryMealGroupDto(string MealSlot, DiaryEntryDto[] Items, string? MealName = null, string? MealId = null);

public record DiaryDayResponse(
    DateOnly Date,
    DiaryMealGroupDto[] Meals,
    MacroTotalsDto Totals);
