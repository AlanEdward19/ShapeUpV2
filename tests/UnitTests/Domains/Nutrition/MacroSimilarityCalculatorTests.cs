using ShapeUp.Features.Nutrition.Shared;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition;

public class MacroSimilarityCalculatorTests
{
    [Fact]
    public void CalculateNormalizedDistance_WhenCandidateMatchesOriginal_ReturnsZero()
    {
        var macros = new MacroValueObject { Kcal = 200, ProteinG = 20, CarbG = 30, FatG = 5 };

        var distance = MacroSimilarityCalculator.CalculateNormalizedDistance(macros, macros);

        Assert.Equal(0d, distance);
    }

    [Fact]
    public void CalculateNormalizedDistance_WhenRankingCandidates_OrdersByLowestDistance()
    {
        var original = new MacroValueObject { Kcal = 100, ProteinG = 10, CarbG = 20, FatG = 5 };
        var closer = new MacroValueObject { Kcal = 110, ProteinG = 11, CarbG = 21, FatG = 5 };
        var farther = new MacroValueObject { Kcal = 200, ProteinG = 5, CarbG = 40, FatG = 10 };

        var closerDistance = MacroSimilarityCalculator.CalculateNormalizedDistance(original, closer);
        var fartherDistance = MacroSimilarityCalculator.CalculateNormalizedDistance(original, farther);

        Assert.True(closerDistance < fartherDistance);
    }
}
