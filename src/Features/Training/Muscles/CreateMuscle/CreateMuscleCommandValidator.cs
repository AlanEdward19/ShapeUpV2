using FluentValidation;

namespace ShapeUp.Features.Training.Muscles.CreateMuscle;

public class CreateMuscleCommandValidator : AbstractValidator<CreateMuscleCommand>
{
    public CreateMuscleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.NamePt).NotEmpty().MaximumLength(120);
    }
}