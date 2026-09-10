namespace ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;

public record MealPlanItemInputDto(string MealSlot, string FoodId, decimal QuantityGramsOrMl);

public record MealPlanItemDto(string MealSlot, string FoodId, decimal QuantityGramsOrMl);

public record MealPlanResponse(
    string Id,
    string Name,
    int? PrescribedByRelationshipId,
    bool IsActive,
    MealPlanItemDto[] Items,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record UnavailableMealPlanItemDto(string MealSlot, string FoodId, decimal QuantityGramsOrMl, string Reason);

public record ActivateMealPlanResponse(
    MealPlanResponse Plan,
    Diary.Shared.ViewModels.DiaryDayResponse DiaryDay,
    UnavailableMealPlanItemDto[] UnavailableItems);
