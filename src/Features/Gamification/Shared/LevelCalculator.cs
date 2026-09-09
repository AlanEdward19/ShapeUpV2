namespace ShapeUp.Features.Gamification.Shared;

public static class LevelCalculator
{
    private const int XpPerLevel = 500;

    public static int CalculateFromTotalXp(int totalXp) =>
        totalXp / XpPerLevel + 1;
}
