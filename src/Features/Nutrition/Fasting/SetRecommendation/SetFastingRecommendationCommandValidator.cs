using FluentValidation;
using ShapeUp.Features.Nutrition.Fasting.Shared;

namespace ShapeUp.Features.Nutrition.Fasting.SetRecommendation;

public sealed class SetFastingRecommendationCommandValidator : AbstractValidator<SetFastingRecommendationCommand>
{
    public SetFastingRecommendationCommandValidator()
    {
        RuleFor(x => x.Protocol)
            .NotEmpty()
            .Must(p => FastingClockCalculator.ParsePresetProtocol(p) is not null)
            .WithMessage("Protocol must be a preset (14:10, 16:8, 18:6, 20:4).");
    }
}
