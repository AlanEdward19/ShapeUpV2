namespace ShapeUp.Features.Nutrition.MealPlanTemplates.AssignMealPlanTemplate;

/// <summary><paramref name="PlanName"/> absent = the template's name.</summary>
public record AssignMealPlanTemplateCommand(string? PlanName = null);
