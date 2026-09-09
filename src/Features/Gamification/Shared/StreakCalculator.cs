namespace ShapeUp.Features.Gamification.Shared;

public static class StreakCalculator
{
    public static int Calculate(DateTime? lastActivityDateUtc, int currentStreak, DateTime activityDateUtc)
    {
        var activityDate = activityDateUtc.Date;

        if (lastActivityDateUtc is null)
            return 1;

        var lastDate = lastActivityDateUtc.Value.Date;

        if (activityDate == lastDate)
            return currentStreak;

        if ((activityDate - lastDate).TotalDays == 1)
            return currentStreak + 1;

        return 1;
    }
}
