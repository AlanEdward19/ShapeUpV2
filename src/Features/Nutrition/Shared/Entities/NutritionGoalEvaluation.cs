namespace ShapeUp.Features.Nutrition.Shared.Entities;

public class NutritionGoalEvaluation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public bool GoalMet { get; set; }
    public DateTime EvaluatedAtUtc { get; set; }
}
