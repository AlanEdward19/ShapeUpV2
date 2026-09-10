namespace ShapeUp.Features.Nutrition.MealPlans.ActivateMealPlan;

public record ActivateMealPlanCommand(string MealPlanId, DateOnly Date);
