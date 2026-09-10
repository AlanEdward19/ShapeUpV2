using FluentValidation;

namespace ShapeUp.Features.Nutrition.Moderation.DecideModeration;

public class DecideModerationCommandValidator : AbstractValidator<DecideModerationCommand>
{
    private static readonly HashSet<string> AllowedDecisions = ["Approved", "Rejected"];

    public DecideModerationCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Decision)
            .NotEmpty()
            .Must(decision => AllowedDecisions.Contains(decision))
            .WithMessage("Decision must be Approved or Rejected.");
    }
}
