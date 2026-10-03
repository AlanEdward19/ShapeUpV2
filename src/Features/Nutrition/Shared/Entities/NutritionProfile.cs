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
    public int? WaterGoalMl { get; set; }
    /// <summary>Free text: dietary restrictions (vegetarian, lactose-free...). Editable by the user and the linked nutritionist.</summary>
    public string? Restrictions { get; set; }
    /// <summary>Free text: food allergies and intolerances.</summary>
    public string? Allergies { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
