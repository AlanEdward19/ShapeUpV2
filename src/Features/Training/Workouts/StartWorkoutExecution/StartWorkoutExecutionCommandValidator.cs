using FluentValidation;

namespace ShapeUp.Features.Training.Workouts.StartWorkoutExecution;

public class StartWorkoutExecutionCommandValidator : AbstractValidator<StartWorkoutExecutionCommand>
{
    public StartWorkoutExecutionCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.StartedAtUtc).NotEmpty();
        RuleFor(x => x.ExecutedByUserId).GreaterThan(0).When(x => x.ExecutedByUserId.HasValue);
    }
}

