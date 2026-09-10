using FluentValidation;

namespace ShapeUp.Features.Nutrition.Profile.CompleteOnboarding;

public class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    private static readonly string[] AllowedSexes = ["Male", "Female"];
    private static readonly string[] AllowedActivityLevels = ["Sedentary", "Light", "Moderate", "Active", "VeryActive"];

    public CompleteOnboardingCommandValidator()
    {
        RuleFor(x => x.HeightCm)
            .InclusiveBetween(100, 250)
            .WithMessage("Height must be between 100 and 250 cm.");

        RuleFor(x => x.Age)
            .InclusiveBetween(13, 120)
            .WithMessage("Age must be between 13 and 120 years.");

        RuleFor(x => x.BiologicalSex)
            .NotEmpty()
            .Must(sex => AllowedSexes.Contains(sex, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Biological sex must be Male or Female.");

        RuleFor(x => x.ActivityLevel)
            .NotEmpty()
            .Must(level => AllowedActivityLevels.Contains(level, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Activity level is invalid.");
    }
}
