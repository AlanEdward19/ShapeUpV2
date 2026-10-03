using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.MealPlanTemplates.Shared.ViewModels;

public record MealPlanTemplateResponse(
    string Id,
    string Name,
    string? Notes,
    MealPlanItemDto[] Items,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
