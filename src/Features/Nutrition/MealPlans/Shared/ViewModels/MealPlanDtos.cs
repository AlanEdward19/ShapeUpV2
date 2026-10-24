namespace ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;

public record MealPlanItemInputDto(string MealSlot, string FoodId, decimal QuantityGramsOrMl);

public record MealPlanItemDto(string MealSlot, string FoodId, decimal QuantityGramsOrMl);

/// <summary>Macro targets of one meal slot; every target is optional but at least one must be set.</summary>
public record MealPlanMealTargetInputDto(string MealSlot, int? TargetCalories, int? TargetProteinG, int? TargetCarbsG, int? TargetFatG);

/// <summary><paramref name="IsMacroOnly"/>: the slot has targets and no items, so the client may structure it freely.</summary>
public record MealPlanMealDto(string MealSlot, int? TargetCalories, int? TargetProteinG, int? TargetCarbsG, int? TargetFatG, bool IsMacroOnly);

public record MealPlanResponse(
    string Id,
    string Name,
    int? PrescribedByRelationshipId,
    bool IsActive,
    MealPlanItemDto[] Items,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int? PrescribedByUserId = null,
    MealPlanMealDto[]? Meals = null,
    bool IsMacroOnly = false);

public record UnavailableMealPlanItemDto(string MealSlot, string FoodId, decimal QuantityGramsOrMl, string Reason);

public record ActivateMealPlanResponse(
    MealPlanResponse Plan,
    Diary.Shared.ViewModels.DiaryDayResponse DiaryDay,
    UnavailableMealPlanItemDto[] UnavailableItems,
    int AppliedItemCount = 0,
    int TotalItemCount = 0);
