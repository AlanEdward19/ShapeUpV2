namespace ShapeUp.Features.Nutrition.Shared;

public static class FoodCategories
{
    public const string Food = "Food";
    public const string Supplement = "Supplement";

    public static bool IsValid(string? category) => category is Food or Supplement;
}
