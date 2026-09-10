using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.GoalEvaluation;

public static class NutritionGoalToleranceCalculator
{
    private const decimal DefaultTolerance = 0.10m;

    public static bool AreMacrosWithinTolerance(MacroValueObject actual, MacroValueObject goal) =>
        IsWithinTolerance(actual.ProteinG, goal.ProteinG)
        && IsWithinTolerance(actual.CarbG, goal.CarbG)
        && IsWithinTolerance(actual.FatG, goal.FatG);

    private static bool IsWithinTolerance(int actual, int goal)
    {
        if (goal == 0)
            return actual == 0;

        var minimum = (int)Math.Floor(goal * (1 - DefaultTolerance));
        var maximum = (int)Math.Ceiling(goal * (1 + DefaultTolerance));
        return actual >= minimum && actual <= maximum;
    }
}
