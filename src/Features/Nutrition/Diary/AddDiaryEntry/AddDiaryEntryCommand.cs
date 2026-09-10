namespace ShapeUp.Features.Nutrition.Diary.AddDiaryEntry;

public record AddDiaryEntryCommand(
    string Id,
    DateOnly Date,
    string MealSlot,
    string FoodId,
    decimal QuantityGramsOrMl);
