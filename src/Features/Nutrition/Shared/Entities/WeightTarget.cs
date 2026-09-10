namespace ShapeUp.Features.Nutrition.Shared.Entities;

public class WeightTarget
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal TargetWeight { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
