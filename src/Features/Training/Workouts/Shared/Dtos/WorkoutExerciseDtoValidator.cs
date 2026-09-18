using FluentValidation;

namespace ShapeUp.Features.Training.Workouts.Shared.Dtos;

public class WorkoutExerciseDtoValidator : AbstractValidator<WorkoutExerciseDto>
{
    public WorkoutExerciseDtoValidator()
    {
        RuleFor(x => x.ExerciseId).GreaterThan(0);
        RuleFor(x => x.Sets).NotEmpty();

        RuleForEach(x => x.Sets).ChildRules(set =>
        {
            // Repetitions/RestSeconds are required only for WeightBased-shaped sets (Load present).
            // A TimeBased set (no Load) has no Repetitions/RestSeconds requirement at this layer --
            // the "duration required for TimeBased" gate needs the exercise's ExerciseType, which
            // this DTO validator has no access to, so it is enforced at the handler level instead.
            set.RuleFor(x => x.Repetitions).NotNull().When(x => x.Load.HasValue);
            set.RuleFor(x => x.Repetitions!.Value).GreaterThan(0).When(x => x.Repetitions.HasValue);
            set.RuleFor(x => x.Load).GreaterThanOrEqualTo(0).When(x => x.Load.HasValue);
            set.RuleFor(x => x.LoadUnit).IsInEnum();
            set.RuleFor(x => x.SetType).IsInEnum();
            // Intensity is optional by default (RPE required only when the exercise's RequireRpe
            // flag is true, enforced at handler level per AD-005) — value range still validated when present.
            set.RuleFor(x => x.Intensity!.Value).InclusiveBetween(1, 10).When(x => x.Intensity != null);
            set.RuleFor(x => x.RestSeconds).NotNull().When(x => x.Load.HasValue);
            set.RuleFor(x => x.RestSeconds!.Value).GreaterThanOrEqualTo(0).When(x => x.RestSeconds.HasValue);
            set.RuleFor(x => x.DurationSeconds!.Value).GreaterThan(0).When(x => x.DurationSeconds.HasValue);
            set.RuleFor(x => x.DistanceMeters!.Value).GreaterThanOrEqualTo(0).When(x => x.DistanceMeters.HasValue);
        });
    }
}
