namespace ShapeUp.Features.Gamification.GetGamificationProfile;

public record GamificationProfileResponse(
    int TotalXp,
    int Level,
    int CurrentStreak,
    int NutritionCurrentStreak,
    int ShapeCoins,
    int ShapeScore,
    bool LastEvaluationLeveledUp,
    int? LastEvaluationLevelFrom,
    int? LastEvaluationLevelTo,
    bool LastEvaluationStreakMilestoneHit,
    int? LastEvaluationStreakMilestoneValue);
