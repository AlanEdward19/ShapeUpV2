using FluentValidation;

namespace ShapeUp.Features.Training.Exercises.CreateExercise;

public class CreateExerciseCommandValidator : AbstractValidator<CreateExerciseCommand>
{
    public CreateExerciseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.NamePt).NotEmpty().MaximumLength(160);
        RuleForEach(x => x.Muscles).ChildRules(m =>
        {
            m.RuleFor(x => x.MuscleId).GreaterThan(0);
            m.RuleFor(x => x.ActivationPercent).InclusiveBetween(0.01m, 100m);
        });
        RuleForEach(x => x.EquipmentIds).GreaterThan(0);
        RuleForEach(x => x.Steps).ChildRules(s => s.RuleFor(x => x.Description).NotEmpty().MaximumLength(500));
    }
}