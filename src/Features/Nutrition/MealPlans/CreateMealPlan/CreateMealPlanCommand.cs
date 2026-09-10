using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;

public record CreateMealPlanCommand(string Name, MealPlanItemInputDto[] Items);
