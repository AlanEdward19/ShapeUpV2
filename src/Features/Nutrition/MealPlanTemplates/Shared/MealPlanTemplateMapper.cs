using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.MealPlanTemplates.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.MealPlanTemplates.Shared;

internal static class MealPlanTemplateMapper
{
    internal static MealPlanTemplateResponse ToResponse(MealPlanTemplateDocument template) =>
        new(
            template.Id,
            template.Name,
            template.Notes,
            template.Items.Select(i => new MealPlanItemDto(i.MealSlot, i.FoodId, i.QuantityGramsOrMl)).ToArray(),
            template.CreatedAtUtc,
            template.UpdatedAtUtc);
}
