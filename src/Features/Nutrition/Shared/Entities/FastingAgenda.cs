namespace ShapeUp.Features.Nutrition.Shared.Entities;

public class FastingAgenda
{
    public const string RecommendationPending = "Pending";
    public const string RecommendationAdopted = "Adopted";
    public const string RecommendationDeclined = "Declined";

    public int UserId { get; set; }
    public int? FastHours { get; set; }
    public int? EatHours { get; set; }
    public string? Protocol { get; set; }
    public int? EatingStartMinutes { get; set; }
    public string? TimeZone { get; set; }
    public int? RecommendedFastHours { get; set; }
    public string? RecommendedProtocol { get; set; }
    /// <summary>Pending | Adopted | Declined. Null on rows written before the lifecycle existed (see FastingMapper).</summary>
    public string? RecommendationStatus { get; set; }
    public int? RecommendedByUserId { get; set; }
    public DateTime? RecommendedAtUtc { get; set; }
    public DateTime? RecommendationDeclinedAtUtc { get; set; }
    public string? RecommendationDeclineReason { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
