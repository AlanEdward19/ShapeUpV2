namespace ShapeUp.Features.Gamification.Shared.Entities;

public class GamificationProfile
{
    public int UserId { get; set; }
    public int TotalXp { get; set; }
    public int Level { get; set; }
    public int CurrentStreak { get; set; }
    public int NutritionCurrentStreak { get; set; }
    public DateOnly? LastNutritionGoalMetDate { get; set; }
    public DateTime? LastActivityDateUtc { get; set; }
    public int ShapeCoins { get; set; }
    public int LastStreakMilestoneAwarded { get; set; }
    public bool LastEvaluationLeveledUp { get; set; }
    public int? LastEvaluationLevelFrom { get; set; }
    public int? LastEvaluationLevelTo { get; set; }
    public bool LastEvaluationStreakMilestoneHit { get; set; }
    public int? LastEvaluationStreakMilestoneValue { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
