namespace ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;

public record MacroGoalDto(int Kcal, int ProteinG, int CarbG, int FatG, int? WaterMl = null);

public record NutritionProfileResponse(
    int? HeightCm,
    int? Age,
    string? BiologicalSex,
    string? ActivityLevel,
    bool OnboardingSkipped,
    MacroGoalDto? ActiveGoal,
    DateTime UpdatedAtUtc,
    string? Restrictions = null,
    string? Allergies = null);

public record SetDietaryRestrictionsCommand(string? Restrictions, string? Allergies);
