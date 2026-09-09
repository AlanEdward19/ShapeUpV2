namespace ShapeUp.Features.Gamification.Shared.Entities;

using Enums;

public class WorkoutEvaluation
{
    public string SessionId { get; set; } = null!;
    public int UserId { get; set; }
    public ActivityClassification Classification { get; set; }
    public string? Reason { get; set; }
    public bool CreditGranted { get; set; }
    public DateTime EvaluatedAtUtc { get; set; }
}
