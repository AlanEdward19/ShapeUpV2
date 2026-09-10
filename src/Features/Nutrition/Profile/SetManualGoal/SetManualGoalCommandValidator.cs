using FluentValidation;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.Profile.SetManualGoal;

public class SetManualGoalCommandValidator : AbstractValidator<SetManualGoalCommand>
{
    public SetManualGoalCommandValidator()
    {
        RuleFor(x => x.Goal.Kcal).GreaterThan(0);
        RuleFor(x => x.Goal.ProteinG).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Goal.CarbG).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Goal.FatG).GreaterThanOrEqualTo(0);
    }
}
