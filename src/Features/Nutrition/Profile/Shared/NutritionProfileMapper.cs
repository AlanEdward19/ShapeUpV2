using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Profile.Shared;

internal static class NutritionProfileMapper
{
    internal static NutritionProfileResponse ToResponse(NutritionProfile profile) =>
        new(
            profile.HeightCm,
            profile.Age,
            profile.BiologicalSex,
            profile.ActivityLevel,
            profile.OnboardingSkipped,
            profile.ActiveGoal is null ? null : ToGoalDto(profile.ActiveGoal, profile.WaterGoalMl),
            profile.UpdatedAtUtc,
            profile.Restrictions,
            profile.Allergies);

    internal static MacroGoalDto ToGoalDto(MacroValueObject goal, int? waterGoalMl) =>
        new(goal.Kcal, goal.ProteinG, goal.CarbG, goal.FatG, waterGoalMl);

    internal static int? ToWaterGoal(int? waterMl) =>
        waterMl is > 0 ? waterMl : null;

    internal static MacroValueObject ToMacroValueObject(MacroGoalDto dto) =>
        new()
        {
            Kcal = dto.Kcal,
            ProteinG = dto.ProteinG,
            CarbG = dto.CarbG,
            FatG = dto.FatG
        };

    internal static MacroValueObject ToMacroValueObject(ShapeUp.Features.Nutrition.Shared.MacroGoal goal) =>
        new()
        {
            Kcal = goal.Kcal,
            ProteinG = goal.ProteinG,
            CarbG = goal.CarbG,
            FatG = goal.FatG
        };
}
