using FluentValidation;

namespace ShapeUp.Features.Training.WorkoutTemplates.AssignWorkoutTemplate;

public class AssignWorkoutTemplateCommandValidator : AbstractValidator<AssignWorkoutTemplateCommand>
{
    public AssignWorkoutTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.TargetUserId).GreaterThan(0);
        RuleFor(x => x.PlanName).MaximumLength(120);
    }
}

