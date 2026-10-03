namespace ShapeUp.Features.Nutrition.Comments.Shared.ViewModels;

public record DiaryCommentResponse(
    int Id,
    DateOnly Date,
    string? EntryId,
    int AuthorUserId,
    string? AuthorName,
    string Text,
    DateTime CreatedAtUtc);
