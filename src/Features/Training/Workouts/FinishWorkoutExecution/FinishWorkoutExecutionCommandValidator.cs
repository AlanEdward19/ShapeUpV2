using FluentValidation;

namespace ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;

public class FinishWorkoutExecutionCommandValidator : AbstractValidator<FinishWorkoutExecutionCommand>
{
    public FinishWorkoutExecutionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.EndedAtUtc).NotEmpty();
        RuleFor(x => x.PerceivedExertion).InclusiveBetween(1, 10);
    }
}
