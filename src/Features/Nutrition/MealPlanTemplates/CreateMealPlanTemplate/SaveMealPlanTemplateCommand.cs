using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.MealPlanTemplates.CreateMealPlanTemplate;

/// <summary>Body of both create and update: the template is replaced as a whole.</summary>
public record SaveMealPlanTemplateCommand(string Name, MealPlanItemInputDto[] Items, string? Notes = null);
