using FluentValidation;

namespace ShapeUp.Features.Training.Workouts.CompleteWorkoutSession;

public class CompleteWorkoutSessionCommandValidator : AbstractValidator<CompleteWorkoutSessionCommand>
{
    public CompleteWorkoutSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.PerceivedExertion).InclusiveBetween(1, 10);
    }
}