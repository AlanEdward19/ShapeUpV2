using FluentValidation;

namespace ShapeUp.Features.Training.Workouts.UpdateWorkoutExecutionState;

public class UpdateWorkoutExecutionStateCommandValidator : AbstractValidator<UpdateWorkoutExecutionStateCommand>
{
    public UpdateWorkoutExecutionStateCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.SavedAtUtc).NotNull();
        RuleFor(x => x.Exercises).NotEmpty();

        RuleForEach(x => x.Exercises).ChildRules(exercise =>
        {
            exercise.RuleFor(x => x.ExerciseId).GreaterThan(0);
            exercise.RuleFor(x => x.Sets).NotEmpty();

            exercise.RuleForEach(x => x.Sets).ChildRules(set =>
            {
                set.RuleFor(x => x.Repetitions).NotNull();
                set.RuleFor(x => x.Repetitions!.Value).GreaterThan(0).When(x => x.Repetitions.HasValue);
                set.RuleFor(x => x.Load).GreaterThanOrEqualTo(0);
                set.RuleFor(x => x.LoadUnit).IsInEnum();
                set.RuleFor(x => x.SetType).IsInEnum();
                set.RuleFor(x => x.Intensity).NotNull();
                set.RuleFor(x => x.Intensity!.Value).InclusiveBetween(1, 10).When(x => x.Intensity != null);
                set.RuleFor(x => x.RestSeconds).NotNull();
                set.RuleFor(x => x.RestSeconds!.Value).GreaterThanOrEqualTo(0).When(x => x.RestSeconds.HasValue);
            });
        });
    }
}
