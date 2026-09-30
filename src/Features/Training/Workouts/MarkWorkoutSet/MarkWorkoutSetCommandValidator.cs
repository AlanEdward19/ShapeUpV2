using FluentValidation;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.Workouts.MarkWorkoutSet;

public class MarkWorkoutSetCommandValidator : AbstractValidator<MarkWorkoutSetCommand>
{
    public MarkWorkoutSetCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.OperationId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ExerciseId).GreaterThan(0);
        RuleFor(x => x.Set).NotNull();

        // Same per-set rules as the other workout endpoints.
        When(x => x.Set is not null, () =>
            RuleFor(x => new WorkoutExerciseDto(x.ExerciseId, new[] { x.Set }))
                .SetValidator(new WorkoutExerciseDtoValidator()));
    }
}
