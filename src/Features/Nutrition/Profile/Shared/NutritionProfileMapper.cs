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
            profile.ActiveGoal is null ? null : ToGoalDto(profile.ActiveGoal),
            profile.UpdatedAtUtc);

    internal static MacroGoalDto ToGoalDto(MacroValueObject goal) =>
        new(goal.Kcal, goal.ProteinG, goal.CarbG, goal.FatG);

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
