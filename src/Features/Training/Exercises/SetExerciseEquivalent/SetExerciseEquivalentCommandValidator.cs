using FluentValidation;

namespace ShapeUp.Features.Training.Exercises.SetExerciseEquivalent;

public class SetExerciseEquivalentCommandValidator : AbstractValidator<SetExerciseEquivalentCommand>
{
    public SetExerciseEquivalentCommandValidator()
    {
        RuleFor(x => x.ExerciseId).GreaterThan(0);
        RuleFor(x => x.EquivalentExerciseId).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.ExerciseId != x.EquivalentExerciseId)
            .WithMessage("An exercise cannot be marked as equivalent to itself.");
    }
}
