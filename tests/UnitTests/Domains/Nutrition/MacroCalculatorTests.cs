using ShapeUp.Features.Nutrition.Shared;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition;

public class MacroCalculatorTests
{
    [Fact]
    public void CalculateFromPer100_WhenQuantityIs150Grams_ScalesMacrosCorrectly()
    {
        var per100 = new MacroValueObject
        {
            Kcal = 100,
            ProteinG = 10,
            CarbG = 20,
            FatG = 5
        };

        var result = MacroCalculator.CalculateFromPer100(per100, 150m);

        Assert.Equal(150, result.Kcal);
        Assert.Equal(15, result.ProteinG);
        Assert.Equal(30, result.CarbG);
        Assert.Equal(8, result.FatG);
    }

    [Fact]
    public void Sum_WhenMultipleEntriesProvided_ReturnsAggregatedTotals()
    {
        var first = new MacroValueObject { Kcal = 100, ProteinG = 10, CarbG = 20, FatG = 5 };
        var second = new MacroValueObject { Kcal = 50, ProteinG = 5, CarbG = 10, FatG = 2 };

        var total = MacroCalculator.Sum([first, second]);

        Assert.Equal(150, total.Kcal);
        Assert.Equal(15, total.ProteinG);
        Assert.Equal(30, total.CarbG);
        Assert.Equal(7, total.FatG);
    }
}
