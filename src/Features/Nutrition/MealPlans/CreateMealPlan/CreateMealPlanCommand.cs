using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;

/// <summary><paramref name="TargetUserId"/> absent = the logged user; present = a plan prescribed for a client.</summary>
public record CreateMealPlanCommand(string Name, MealPlanItemInputDto[] Items,
    int? TargetUserId = null);
