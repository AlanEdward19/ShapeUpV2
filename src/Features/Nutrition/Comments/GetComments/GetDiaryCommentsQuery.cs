namespace ShapeUp.Features.Nutrition.Comments.GetComments;

/// <summary>
/// One day with <paramref name="Date"/>, or a range with From/To (inclusive, at most
/// <see cref="GetDiaryCommentsHandler.MaxRangeDays"/> days). Oldest first, paginated by cursor.
/// </summary>
public record GetDiaryCommentsQuery(
    DateOnly? Date = null, DateOnly? From = null, DateOnly? To = null, string? Cursor = null, int? PageSize = null);
