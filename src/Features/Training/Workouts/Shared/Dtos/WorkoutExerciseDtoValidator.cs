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
            set.RuleFor(x => x.Repetitions).NotNull();
            set.RuleFor(x => x.Repetitions!.Value).GreaterThan(0).When(x => x.Repetitions.HasValue);
            set.RuleFor(x => x.Load).GreaterThanOrEqualTo(0);
            set.RuleFor(x => x.LoadUnit).IsInEnum();
            set.RuleFor(x => x.SetType).IsInEnum();
            // Intensity is optional by default (RPE required only when the exercise's RequireRpe
            // flag is true, enforced at handler level per AD-005) — value range still validated when present.
            set.RuleFor(x => x.Intensity!.Value).InclusiveBetween(1, 10).When(x => x.Intensity != null);
            set.RuleFor(x => x.RestSeconds).NotNull();
            set.RuleFor(x => x.RestSeconds!.Value).GreaterThanOrEqualTo(0).When(x => x.RestSeconds.HasValue);
        });
    }
}
