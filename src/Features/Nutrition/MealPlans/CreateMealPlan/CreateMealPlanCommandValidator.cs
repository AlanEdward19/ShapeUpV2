using FluentValidation;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;

public class CreateMealPlanCommandValidator : AbstractValidator<CreateMealPlanCommand>
{
    private static readonly string[] AllowedMealSlots = ["breakfast", "lunch", "dinner", "snack"];

    public CreateMealPlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Items).NotEmpty();
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
