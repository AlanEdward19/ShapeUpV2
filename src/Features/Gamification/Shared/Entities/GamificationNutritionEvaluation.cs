namespace ShapeUp.Features.Gamification.Shared.Entities;

public class GamificationNutritionEvaluation
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CreditedAtUtc { get; set; }
}
