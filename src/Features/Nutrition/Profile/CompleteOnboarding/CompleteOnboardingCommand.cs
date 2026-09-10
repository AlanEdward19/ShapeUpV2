namespace ShapeUp.Features.Nutrition.Profile.CompleteOnboarding;

public record CompleteOnboardingCommand(
    int HeightCm,
    int Age,
    string BiologicalSex,
    string ActivityLevel);
