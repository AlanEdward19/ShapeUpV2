using FluentValidation;

namespace ShapeUp.Features.Nutrition.MealPlanTemplates.CreateMealPlanTemplate;

public class SaveMealPlanTemplateCommandValidator : AbstractValidator<SaveMealPlanTemplateCommand>
{
    private static readonly string[] AllowedMealSlots = ["breakfast", "lunch", "dinner", "snack"];

    public SaveMealPlanTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
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
