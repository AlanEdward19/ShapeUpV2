using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Shared.Entities;

public class DiaryEntry
{
    public string Id { get; set; } = null!;
    public int DiaryDayId { get; set; }
    public DiaryDay DiaryDay { get; set; } = null!;
    public string MealSlot { get; set; } = null!;
    /// <summary>The <see cref="Meals.UserMeal"/> this entry belongs to; null for legacy slot-only entries.</summary>
    public string? MealId { get; set; }
    public string? MealName { get; set; }
    public string FoodId { get; set; } = null!;
    public bool UsesOverride { get; set; }
    public decimal QuantityGramsOrMl { get; set; }
    public MacroValueObject ComputedMacros { get; set; } = null!;
}
