namespace UnitTests.Domains.Gamification;

using ShapeUp.Features.Gamification.Shared;

public class StreakCalculatorTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 9, 14, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Calculate_WhenNoPriorActivity_ReturnsStreakOfOne()
    {
        var streak = StreakCalculator.Calculate(null, 0, BaseDate);

        Assert.Equal(1, streak);
    }

    [Fact]
    public void Calculate_WhenNextCalendarDay_IncrementsStreak()
    {
        var lastActivity = BaseDate.AddDays(-1);
        var streak = StreakCalculator.Calculate(lastActivity, 3, BaseDate);

        Assert.Equal(4, streak);
    }

    [Fact]
    public void Calculate_WhenSameUtcDay_KeepsCurrentStreak()
    {
        var lastActivity = BaseDate.Date;
        var streak = StreakCalculator.Calculate(lastActivity, 5, BaseDate.AddHours(6));

        Assert.Equal(5, streak);
    }

    [Fact]
    public void Calculate_WhenGapGreaterThanOneDay_ResetsStreakToOne()
    {
        var lastActivity = BaseDate.AddDays(-2);
        var streak = StreakCalculator.Calculate(lastActivity, 7, BaseDate);

        Assert.Equal(1, streak);
    }
}
