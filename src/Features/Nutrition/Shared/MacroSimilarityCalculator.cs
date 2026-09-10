using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Shared;

public static class MacroSimilarityCalculator
{
    public static double CalculateNormalizedDistance(MacroValueObject original, MacroValueObject candidate)
    {
        var squaredSum = 0d;
        squaredSum += NormalizedComponent(original.Kcal, candidate.Kcal);
        squaredSum += NormalizedComponent(original.ProteinG, candidate.ProteinG);
        squaredSum += NormalizedComponent(original.CarbG, candidate.CarbG);
        squaredSum += NormalizedComponent(original.FatG, candidate.FatG);

        return Math.Sqrt(squaredSum);
    }

    private static double NormalizedComponent(int original, int candidate)
    {
        if (original == 0)
            return candidate == 0 ? 0d : 1d;

        var delta = (candidate - (double)original) / original;
        return delta * delta;
    }
}
