using FluentValidation;

namespace ShapeUp.Features.Training.Exercises.SuggestExercise;

public class SuggestExercisesQueryValidator : AbstractValidator<SuggestExercisesQuery>
{
    public SuggestExercisesQueryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleForEach(x => x.MuscleIds).GreaterThan(0);
        RuleForEach(x => x.EquipmentIds).GreaterThan(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(30).When(x => x.Limit.HasValue);
    }
}