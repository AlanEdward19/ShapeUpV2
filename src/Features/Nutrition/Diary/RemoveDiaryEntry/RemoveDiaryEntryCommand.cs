namespace ShapeUp.Features.Nutrition.Diary.RemoveDiaryEntry;

public record RemoveDiaryEntryCommand(string EntryId, DateOnly Date);
