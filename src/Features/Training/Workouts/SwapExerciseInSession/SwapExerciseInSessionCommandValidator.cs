using FluentValidation;
using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Workouts.SwapExerciseInSession;

public class SwapExerciseInSessionCommandValidator : AbstractValidator<SwapExerciseInSessionCommand>
{
    public SwapExerciseInSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.OriginalExerciseId).GreaterThan(0);
        RuleFor(x => x.NewExerciseId).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.OriginalExerciseId != x.NewExerciseId)
            .WithMessage("Original and new exercise must be different.");
        RuleFor(x => x.RetainedSetsForOriginal).NotNull();

        RuleForEach(x => x.RetainedSetsForOriginal).ChildRules(set =>
        {
            set.RuleFor(x => x.Repetitions!.Value).GreaterThan(0).When(x => x.Repetitions.HasValue);
            set.RuleFor(x => x.Load).GreaterThanOrEqualTo(0);
            set.RuleFor(x => x.LoadUnit).IsInEnum();
            set.RuleFor(x => x.SetType).IsInEnum();
            set.RuleFor(x => x.Technique).IsInEnum();
            set.RuleFor(x => x.Intensity!.Value).InclusiveBetween(1, 10).When(x => x.Intensity != null);
            set.RuleFor(x => x.RestSeconds!.Value).GreaterThanOrEqualTo(0).When(x => x.RestSeconds.HasValue);
        });
    }
}
