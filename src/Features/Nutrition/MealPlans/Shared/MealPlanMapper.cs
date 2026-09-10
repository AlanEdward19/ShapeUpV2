using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.MealPlans.Shared;

internal static class MealPlanMapper
{
    internal static MealPlanResponse ToResponse(MealPlanDocument plan) =>
        new(
            plan.Id,
            plan.Name,
            plan.PrescribedByRelationshipId,
            plan.IsActive,
            plan.Items.Select(i => new MealPlanItemDto(i.MealSlot, i.FoodId, i.QuantityGramsOrMl)).ToArray(),
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc);
}
