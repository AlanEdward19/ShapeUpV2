namespace ShapeUp.Features.PlatformFeatureFlags.SetFeatureFlag;

using FluentValidation;

public class SetFeatureFlagCommandValidator : AbstractValidator<SetFeatureFlagCommand>
{
    public SetFeatureFlagCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(128);
    }
}
