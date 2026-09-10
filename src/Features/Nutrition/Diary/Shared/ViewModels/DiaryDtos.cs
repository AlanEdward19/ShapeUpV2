namespace ShapeUp.Features.Nutrition.Diary.Shared.ViewModels;

public record MacroTotalsDto(int Kcal, int ProteinG, int CarbG, int FatG);

public record DiaryEntryDto(
    string Id,
    string MealSlot,
    string FoodId,
    bool UsesOverride,
    decimal QuantityGramsOrMl,
    MacroTotalsDto ComputedMacros);

public record DiaryMealGroupDto(string MealSlot, DiaryEntryDto[] Items);

public record DiaryDayResponse(
    DateOnly Date,
    DiaryMealGroupDto[] Meals,
    MacroTotalsDto Totals);
