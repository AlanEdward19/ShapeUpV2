using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Shared.Entities;

public class NutritionProfile
{
    public int UserId { get; set; }
    public int? HeightCm { get; set; }
    public int? Age { get; set; }
    public string? BiologicalSex { get; set; }
    public string? ActivityLevel { get; set; }
    public bool OnboardingSkipped { get; set; }
    public MacroValueObject? ActiveGoal { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
