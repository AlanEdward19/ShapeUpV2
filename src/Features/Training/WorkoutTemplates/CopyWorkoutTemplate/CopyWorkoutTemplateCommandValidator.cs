using FluentValidation;

namespace ShapeUp.Features.Training.WorkoutTemplates.CopyWorkoutTemplate;

public class CopyWorkoutTemplateCommandValidator : AbstractValidator<CopyWorkoutTemplateCommand>
{
    public CopyWorkoutTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(120);
    }
}

