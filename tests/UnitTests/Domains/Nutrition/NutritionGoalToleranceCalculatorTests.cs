using ShapeUp.Features.Nutrition.GoalEvaluation;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition;

public class NutritionGoalToleranceCalculatorTests
{
    private static readonly MacroValueObject Goal = new()
    {
        Kcal = 2000,
        ProteinG = 100,
        CarbG = 200,
        FatG = 70
    };

    [Fact]
    public void AreMacrosWithinTolerance_WhenAllThreeMacrosWithinTenPercent_ReturnsTrue()
    {
        var actual = new MacroValueObject
        {
            Kcal = 2100,
            ProteinG = 100,
            CarbG = 200,
            FatG = 70
        };

        Assert.True(NutritionGoalToleranceCalculator.AreMacrosWithinTolerance(actual, Goal));
    }

    [Fact]
    public void AreMacrosWithinTolerance_WhenProteinAtLowerBoundary_ReturnsTrue()
    {
        var actual = new MacroValueObject
        {
            Kcal = 0,
            ProteinG = 90,
            CarbG = 200,
            FatG = 70
        };

        Assert.True(NutritionGoalToleranceCalculator.AreMacrosWithinTolerance(actual, Goal));
    }

    [Fact]
    public void AreMacrosWithinTolerance_WhenProteinAtUpperBoundary_ReturnsTrue()
    {
        var actual = new MacroValueObject
        {
            Kcal = 0,
            ProteinG = 110,
            CarbG = 200,
            FatG = 70
        };

        Assert.True(NutritionGoalToleranceCalculator.AreMacrosWithinTolerance(actual, Goal));
    }

    [Fact]
    public void AreMacrosWithinTolerance_WhenAnyMacroOutsideTenPercent_ReturnsFalse()
    {
        var actual = new MacroValueObject
        {
            Kcal = 0,
            ProteinG = 89,
            CarbG = 200,
            FatG = 70
        };

        Assert.False(NutritionGoalToleranceCalculator.AreMacrosWithinTolerance(actual, Goal));
    }

    [Fact]
    public void AreMacrosWithinTolerance_WhenKcalOutsideToleranceButMacrosMatch_IgnoresKcal()
    {
        var actual = new MacroValueObject
        {
            Kcal = 5000,
            ProteinG = 100,
            CarbG = 200,
            FatG = 70
        };

        Assert.True(NutritionGoalToleranceCalculator.AreMacrosWithinTolerance(actual, Goal));
    }
}
