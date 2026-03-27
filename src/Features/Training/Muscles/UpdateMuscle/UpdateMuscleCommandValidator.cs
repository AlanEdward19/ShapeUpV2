using FluentValidation;

namespace ShapeUp.Features.Training.Muscles.UpdateMuscle;

public class UpdateMuscleCommandValidator : AbstractValidator<UpdateMuscleCommand>
{
    public UpdateMuscleCommandValidator()
    {
        RuleFor(x => x.MuscleId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.NamePt).NotEmpty().MaximumLength(120);
    }
}