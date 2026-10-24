using FluentValidation;
using ShapeUp.Features.Nutrition.Fasting.Shared;

namespace ShapeUp.Features.Nutrition.Fasting.DeclineRecommendation;

public sealed record DeclineFastingRecommendationCommand(string? Reason = null);

public sealed class DeclineFastingRecommendationCommandValidator : AbstractValidator<DeclineFastingRecommendationCommand>
{
    public DeclineFastingRecommendationCommandValidator() =>
        RuleFor(x => x.Reason).Must(r => (r?.Trim().Length ?? 0) <= FastingMapper.MaxReasonLength)
            .WithMessage($"Reason must have at most {FastingMapper.MaxReasonLength} characters.");
}
