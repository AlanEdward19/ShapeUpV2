namespace ShapeUp.Features.Nutrition.Comments.GetComments;

/// <summary>One day with <paramref name="Date"/>, or a range with From/To (inclusive).</summary>
public record GetDiaryCommentsQuery(DateOnly? Date = null, DateOnly? From = null, DateOnly? To = null);
