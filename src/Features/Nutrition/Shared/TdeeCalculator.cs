using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Shared;

public record MacroGoal(int Kcal, int ProteinG, int CarbG, int FatG);

public static class TdeeCalculator
{
    private static readonly IReadOnlyDictionary<string, decimal> ActivityFactors =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sedentary"] = 1.2m,
            ["Light"] = 1.375m,
            ["Moderate"] = 1.55m,
            ["Active"] = 1.725m,
            ["VeryActive"] = 1.9m
        };

    public static MacroGoal Calculate(NutritionProfile profile, decimal currentWeightKg)
    {
        if (profile.HeightCm is null or <= 0 ||
            profile.Age is null or <= 0 ||
            string.IsNullOrWhiteSpace(profile.BiologicalSex) ||
            string.IsNullOrWhiteSpace(profile.ActivityLevel) ||
            currentWeightKg <= 0)
        {
            throw new InvalidOperationException("Nutrition profile is incomplete for TDEE calculation.");
        }

        if (!ActivityFactors.TryGetValue(profile.ActivityLevel, out var activityFactor))
            throw new InvalidOperationException($"Unknown activity level '{profile.ActivityLevel}'.");

        var bmr = CalculateBmr(currentWeightKg, profile.HeightCm.Value, profile.Age.Value, profile.BiologicalSex);
        var tdeeKcal = (int)Math.Round(bmr * activityFactor, MidpointRounding.AwayFromZero);

        return DeriveMacroGoal(tdeeKcal);
    }

    internal static decimal CalculateBmr(decimal weightKg, int heightCm, int age, string biologicalSex)
    {
        var baseBmr = 10m * weightKg + 6.25m * heightCm - 5m * age;
        return string.Equals(biologicalSex, "Female", StringComparison.OrdinalIgnoreCase)
            ? baseBmr - 161m
            : baseBmr + 5m;
    }

    internal static MacroGoal DeriveMacroGoal(int tdeeKcal)
    {
        var proteinG = (int)Math.Round(tdeeKcal * 0.30m / 4m, MidpointRounding.AwayFromZero);
        var carbG = (int)Math.Round(tdeeKcal * 0.40m / 4m, MidpointRounding.AwayFromZero);
        var fatG = (int)Math.Round(tdeeKcal * 0.30m / 9m, MidpointRounding.AwayFromZero);

        return new MacroGoal(tdeeKcal, proteinG, carbG, fatG);
    }
}
