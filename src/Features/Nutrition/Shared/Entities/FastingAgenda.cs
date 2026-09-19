namespace ShapeUp.Features.Nutrition.Shared.Entities;

public class FastingAgenda
{
    public int UserId { get; set; }
    public int? FastHours { get; set; }
    public int? EatHours { get; set; }
    public string? Protocol { get; set; }
    public int? EatingStartMinutes { get; set; }
    public string? TimeZone { get; set; }
    public int? RecommendedFastHours { get; set; }
    public string? RecommendedProtocol { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
