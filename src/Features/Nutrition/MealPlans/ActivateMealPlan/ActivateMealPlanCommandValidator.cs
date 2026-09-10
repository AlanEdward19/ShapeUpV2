using FluentValidation;

namespace ShapeUp.Features.Nutrition.MealPlans.ActivateMealPlan;

public class ActivateMealPlanCommandValidator : AbstractValidator<ActivateMealPlanCommand>
{
    public ActivateMealPlanCommandValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty().MaximumLength(24);
        RuleFor(x => x.Date).NotEmpty();
    }
}
