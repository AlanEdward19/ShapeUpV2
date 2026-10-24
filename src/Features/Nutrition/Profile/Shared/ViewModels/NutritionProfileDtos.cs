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
    string? Allergies = null,
    bool LinkedToNutritionist = false,
    NutritionistLinkDto? Nutritionist = null);

/// <summary>The nutritionist that currently has access to the user's nutrition data (the oldest link when there are several).</summary>
public record NutritionistLinkDto(int UserId, string? DisplayName);

public record SetDietaryRestrictionsCommand(string? Restrictions, string? Allergies);
