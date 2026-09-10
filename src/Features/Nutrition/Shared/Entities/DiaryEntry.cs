using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Shared.Entities;

public class DiaryEntry
{
    public string Id { get; set; } = null!;
    public int DiaryDayId { get; set; }
    public DiaryDay DiaryDay { get; set; } = null!;
    public string MealSlot { get; set; } = null!;
    public string FoodId { get; set; } = null!;
    public bool UsesOverride { get; set; }
    public decimal QuantityGramsOrMl { get; set; }
    public MacroValueObject ComputedMacros { get; set; } = null!;
}
