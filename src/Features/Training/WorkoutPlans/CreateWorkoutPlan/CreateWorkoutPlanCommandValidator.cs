using FluentValidation;

namespace ShapeUp.Features.Training.WorkoutPlans.CreateWorkoutPlan;

public class CreateWorkoutPlanCommandValidator : AbstractValidator<CreateWorkoutPlanCommand>
{
    public CreateWorkoutPlanCommandValidator()
    {
        RuleFor(x => x.TargetUserId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Exercises).NotEmpty();

        RuleForEach(x => x.Exercises).ChildRules(exercise =>
        {
            exercise.RuleFor(x => x.ExerciseId).GreaterThan(0);
            exercise.RuleFor(x => x.Sets).NotEmpty();

            exercise.RuleForEach(x => x.Sets).ChildRules(set =>
            {
                set.RuleFor(x => x.Repetitions).GreaterThan(0);
                set.RuleFor(x => x.Load).GreaterThanOrEqualTo(0);
                set.RuleFor(x => x.LoadUnit).IsInEnum();
                set.RuleFor(x => x.SetType).IsInEnum();
                set.RuleFor(x => x.Rpe).InclusiveBetween(1, 10);
                set.RuleFor(x => x.RestSeconds).GreaterThanOrEqualTo(0);
            });
        });
    }
}

