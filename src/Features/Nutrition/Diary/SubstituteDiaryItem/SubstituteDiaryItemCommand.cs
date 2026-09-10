namespace ShapeUp.Features.Nutrition.Diary.SubstituteDiaryItem;

public record SubstituteDiaryItemCommand(
    DateOnly Date,
    string EntryId,
    string ReplacementFoodId,
    decimal QuantityGramsOrMl);
