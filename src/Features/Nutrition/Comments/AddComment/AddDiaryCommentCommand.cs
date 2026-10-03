namespace ShapeUp.Features.Nutrition.Comments.AddComment;

/// <summary><paramref name="EntryId"/> absent = comment on the whole day.</summary>
public record AddDiaryCommentCommand(DateOnly Date, string Text, string? EntryId = null);
