using ShapeUp.Features.Gamification.Shared;

namespace UnitTests.Domains.Gamification;

public class LevelCalculatorTests
{
    [Fact]
    public void CalculateFromTotalXp_WhenXpIsZero_ReturnsLevelOne()
    {
        Assert.Equal(1, LevelCalculator.CalculateFromTotalXp(0));
    }

    [Fact]
    public void CalculateFromTotalXp_WhenXpIsExactlyAtThreshold_ReturnsNextLevel()
    {
        Assert.Equal(2, LevelCalculator.CalculateFromTotalXp(500));
    }

    [Fact]
    public void CalculateFromTotalXp_WhenXpIsMidLevel_ReturnsCorrectLevel()
    {
        Assert.Equal(3, LevelCalculator.CalculateFromTotalXp(1250));
    }
}
