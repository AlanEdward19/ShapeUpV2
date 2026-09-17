using FluentValidation;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;

public class FinishWorkoutExecutionCommandValidator : AbstractValidator<FinishWorkoutExecutionCommand>
{
    public FinishWorkoutExecutionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.EndedAtUtc).NotNull();
        RuleFor(x => x.PerceivedExertion).InclusiveBetween(1, 10);

        RuleForEach(x => x.Exercises).SetValidator(new WorkoutExerciseDtoValidator()).When(x => x.Exercises is not null);
    }
}
