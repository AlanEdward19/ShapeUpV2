using FluentValidation;

namespace ShapeUp.Features.Training.Workouts.UpdateWorkoutExecutionState;

public class UpdateWorkoutExecutionStateCommandValidator : AbstractValidator<UpdateWorkoutExecutionStateCommand>
{
    private static readonly string[] AllowedSetTypes = ["warmup", "feeder", "working", "topset", "dropset", "backoff"];
    private static readonly string[] AllowedLoadUnits = ["kg", "lbs"];

    public UpdateWorkoutExecutionStateCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.SavedAtUtc).NotEmpty();
        RuleFor(x => x.Exercises).NotEmpty();

        RuleForEach(x => x.Exercises).ChildRules(exercise =>
        {
            exercise.RuleFor(x => x.ExerciseId).GreaterThan(0);
            exercise.RuleFor(x => x.Sets).NotEmpty();

            exercise.RuleForEach(x => x.Sets).ChildRules(set =>
            {
                set.RuleFor(x => x.Repetitions).GreaterThan(0);
                set.RuleFor(x => x.Load).GreaterThanOrEqualTo(0);
                set.RuleFor(x => x.LoadUnit).Must(x => AllowedLoadUnits.Contains(x.ToLowerInvariant()));
                set.RuleFor(x => x.SetType).Must(x => AllowedSetTypes.Contains(x.ToLowerInvariant()));
                set.RuleFor(x => x.Rpe).InclusiveBetween(1, 10);
                set.RuleFor(x => x.RestSeconds).GreaterThanOrEqualTo(0);
            });
        });
    }
}
