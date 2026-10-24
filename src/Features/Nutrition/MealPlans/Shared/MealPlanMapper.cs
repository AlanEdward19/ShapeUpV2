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
            plan.UpdatedAtUtc,
            plan.PrescribedByUserId,
            ToMealDtos(plan),
            IsMacroOnly(plan));

    /// <summary>One entry per slot that has targets; a slot is macros-only when no item uses it.</summary>
    internal static MealPlanMealDto[] ToMealDtos(MealPlanDocument plan) =>
        plan.Meals
            .Select(m => new MealPlanMealDto(
                m.MealSlot, m.TargetCalories, m.TargetProteinG, m.TargetCarbsG, m.TargetFatG,
                !plan.Items.Any(i => string.Equals(i.MealSlot, m.MealSlot, StringComparison.OrdinalIgnoreCase))))
            .ToArray();

    /// <summary>A plan is macros-only when it has per-meal targets and no items at all.</summary>
    internal static bool IsMacroOnly(MealPlanDocument plan) =>
        plan.Items.Count == 0 && plan.Meals.Count > 0;

    internal static string NormalizeMealSlot(string mealSlot) =>
        mealSlot.ToLowerInvariant() switch
        {
            "breakfast" => "breakfast",
            "lunch" => "lunch",
            "dinner" => "dinner",
            _ => "snack"
        };
}
