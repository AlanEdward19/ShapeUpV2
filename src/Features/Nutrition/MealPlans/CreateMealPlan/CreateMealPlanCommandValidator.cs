using FluentValidation;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;

public class CreateMealPlanCommandValidator : AbstractValidator<CreateMealPlanCommand>
{
    private static readonly string[] AllowedMealSlots = ["breakfast", "lunch", "dinner", "snack"];

    public CreateMealPlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Id).Matches("^[0-9a-fA-F]{24}$").When(x => x.Id != null)
            .WithMessage("Id must be a 24-character hexadecimal string.");
        RuleFor(x => x.Items).NotNull();
        RuleFor(x => x).Must(x => x.Items.Length > 0 || (x.Meals?.Length ?? 0) > 0)
            .When(x => x.Items != null)
            .WithMessage("A meal plan needs items or per-meal macro targets.");
        RuleFor(x => x.Meals).Must(meals => meals!
                .Select(m => m.MealSlot?.ToLowerInvariant()).Distinct().Count() == meals!.Length)
            .When(x => x.Meals != null)
            .WithMessage("Each meal slot may have only one macro target.");
        RuleForEach(x => x.Meals).ChildRules(meal =>
        {
            meal.RuleFor(x => x.MealSlot)
                .NotEmpty()
                .Must(slot => AllowedMealSlots.Contains(slot, StringComparer.OrdinalIgnoreCase));
            meal.RuleFor(x => x).Must(x => x.TargetCalories.HasValue || x.TargetProteinG.HasValue
                    || x.TargetCarbsG.HasValue || x.TargetFatG.HasValue)
                .WithMessage("A meal target needs at least one macro value.");
            meal.RuleFor(x => x.TargetCalories).InclusiveBetween(0, 20000).When(x => x.TargetCalories.HasValue);
            meal.RuleFor(x => x.TargetProteinG).InclusiveBetween(0, 2000).When(x => x.TargetProteinG.HasValue);
            meal.RuleFor(x => x.TargetCarbsG).InclusiveBetween(0, 2000).When(x => x.TargetCarbsG.HasValue);
            meal.RuleFor(x => x.TargetFatG).InclusiveBetween(0, 2000).When(x => x.TargetFatG.HasValue);
        }).When(x => x.Meals != null);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.MealSlot)
                .NotEmpty()
                .Must(slot => AllowedMealSlots.Contains(slot, StringComparer.OrdinalIgnoreCase));
            item.RuleFor(x => x.FoodId).NotEmpty().MaximumLength(24);
            item.RuleFor(x => x.QuantityGramsOrMl).GreaterThan(0);
        });
    }
}
