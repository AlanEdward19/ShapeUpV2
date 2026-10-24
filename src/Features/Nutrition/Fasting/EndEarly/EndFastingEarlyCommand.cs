using FluentValidation;
using ShapeUp.Features.Nutrition.Fasting.Shared;

namespace ShapeUp.Features.Nutrition.Fasting.EndEarly;

public sealed record EndFastingEarlyCommand(string? Reason = null);

public sealed class EndFastingEarlyCommandValidator : AbstractValidator<EndFastingEarlyCommand>
{
    public EndFastingEarlyCommandValidator() =>
        RuleFor(x => x.Reason).Must(r => (r?.Trim().Length ?? 0) <= FastingMapper.MaxReasonLength)
            .WithMessage($"Reason must have at most {FastingMapper.MaxReasonLength} characters.");
}
