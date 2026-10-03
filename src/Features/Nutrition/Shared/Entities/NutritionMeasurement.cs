namespace ShapeUp.Features.Nutrition.Shared.Entities;

/// <summary>Anthropometric measurement of a user, usually recorded by the linked nutritionist.</summary>
public class NutritionMeasurement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? BodyFatPercent { get; set; }
    public decimal? WaistCm { get; set; }
    public decimal? HipCm { get; set; }
    public string? Notes { get; set; }
    public int RecordedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
