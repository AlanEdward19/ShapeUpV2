namespace ShapeUp.Features.Gamification.Shared;

public static class NutritionStreakCalculator
{
    public static int CalculateStoredStreak(DateOnly? lastMetDate, int currentStreak, DateOnly goalMetDate)
    {
        if (lastMetDate is null)
            return 1;

        if (lastMetDate == goalMetDate)
            return currentStreak;

        if (lastMetDate == goalMetDate.AddDays(-1))
            return currentStreak + 1;

        return 1;
    }

    public static int DeriveDisplayedStreak(int storedStreak, DateOnly? lastMetDate, DateOnly today)
    {
        if (lastMetDate is null)
            return 0;

        if (lastMetDate == today || lastMetDate == today.AddDays(-1))
            return storedStreak;

        return 0;
    }
}
