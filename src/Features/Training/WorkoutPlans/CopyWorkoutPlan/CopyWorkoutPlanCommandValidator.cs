using FluentValidation;

namespace ShapeUp.Features.Training.WorkoutPlans.CopyWorkoutPlan;

public class CopyWorkoutPlanCommandValidator : AbstractValidator<CopyWorkoutPlanCommand>
{
    public CopyWorkoutPlanCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.TargetUserId).GreaterThan(0).When(x => x.TargetUserId.HasValue);
        RuleFor(x => x.Name).MaximumLength(120);
    }
}

