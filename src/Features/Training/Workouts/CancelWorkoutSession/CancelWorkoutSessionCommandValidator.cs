using FluentValidation;

namespace ShapeUp.Features.Training.Workouts.CancelWorkoutSession;

public class CancelWorkoutSessionCommandValidator : AbstractValidator<CancelWorkoutSessionCommand>
{
    public CancelWorkoutSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}

