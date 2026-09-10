namespace ShapeUp.Features.Nutrition.Foods.SearchFoods;

public record SearchFoodsQuery(string? Query, string? Cursor, int? PageSize);
