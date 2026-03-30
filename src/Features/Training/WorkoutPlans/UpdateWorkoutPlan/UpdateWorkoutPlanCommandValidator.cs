using FluentValidation;

namespace ShapeUp.Features.Training.WorkoutPlans.UpdateWorkoutPlan;

public class UpdateWorkoutPlanCommandValidator : AbstractValidator<UpdateWorkoutPlanCommand>
{
    public UpdateWorkoutPlanCommandValidator()
    {
        RuleFor(x => x.GetPlanId()).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.DurationInWeeks).GreaterThan(0).LessThanOrEqualTo(52);
        RuleFor(x => x.Phase).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Exercises).NotEmpty().WithMessage("Workout plan must have at least one exercise");
    }
}

