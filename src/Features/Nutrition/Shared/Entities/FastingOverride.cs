namespace ShapeUp.Features.Nutrition.Shared.Entities;

public class FastingOverride
{
    public const string StatusFasting = "Fasting";
    public const string StatusEating = "Eating";
    public const string StatusCompleted = "Completed";
    public const string StatusCancelled = "Cancelled";

    public Guid Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = StatusFasting;
    public string Protocol { get; set; } = string.Empty;
    public int FastHours { get; set; }
    public int EatHours { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime FastEndsAtUtc { get; set; }
    public DateTime? EatEndsAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
