namespace ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;

public record MacroGoalDto(int Kcal, int ProteinG, int CarbG, int FatG);

public record NutritionProfileResponse(
    int? HeightCm,
    int? Age,
    string? BiologicalSex,
    string? ActivityLevel,
    bool OnboardingSkipped,
    MacroGoalDto? ActiveGoal,
    DateTime UpdatedAtUtc);
