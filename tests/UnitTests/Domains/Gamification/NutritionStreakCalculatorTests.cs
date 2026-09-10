using ShapeUp.Features.Gamification.Shared;

namespace UnitTests.Domains.Gamification;

public class NutritionStreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 5, 15);

    [Fact]
    public void DeriveDisplayedStreak_WhenLastMetDateIsToday_ReturnsStoredStreak()
    {
        var streak = NutritionStreakCalculator.DeriveDisplayedStreak(4, Today, Today);

        Assert.Equal(4, streak);
    }

    [Fact]
    public void DeriveDisplayedStreak_WhenLastMetDateIsYesterday_ReturnsStoredStreak()
    {
        var streak = NutritionStreakCalculator.DeriveDisplayedStreak(4, Today.AddDays(-1), Today);

        Assert.Equal(4, streak);
    }

    [Fact]
    public void DeriveDisplayedStreak_WhenLastMetDateIsOlderThanYesterday_ReturnsZero()
    {
        var streak = NutritionStreakCalculator.DeriveDisplayedStreak(7, Today.AddDays(-2), Today);

        Assert.Equal(0, streak);
    }

    [Fact]
    public void DeriveDisplayedStreak_WhenLastMetDateMissing_ReturnsZero()
    {
        var streak = NutritionStreakCalculator.DeriveDisplayedStreak(5, null, Today);

        Assert.Equal(0, streak);
    }

    [Fact]
    public void CalculateStoredStreak_WhenPreviousDayWasMet_IncrementsStreak()
    {
        var streak = NutritionStreakCalculator.CalculateStoredStreak(Today.AddDays(-1), 3, Today);

        Assert.Equal(4, streak);
    }

    [Fact]
    public void CalculateStoredStreak_WhenGapIsGreaterThanOneDay_StartsAtOne()
    {
        var streak = NutritionStreakCalculator.CalculateStoredStreak(Today.AddDays(-3), 8, Today);

        Assert.Equal(1, streak);
    }
}
