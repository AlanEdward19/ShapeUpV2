using FluentValidation;

namespace ShapeUp.Features.Training.Exercises.RemoveExerciseEquivalent;

public class RemoveExerciseEquivalentCommandValidator : AbstractValidator<RemoveExerciseEquivalentCommand>
{
    public RemoveExerciseEquivalentCommandValidator()
    {
        RuleFor(x => x.ExerciseId).GreaterThan(0);
        RuleFor(x => x.EquivalentExerciseId).GreaterThan(0);
    }
}
