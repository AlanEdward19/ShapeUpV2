namespace ShapeUp.Features.Nutrition.Moderation.GetPendingModerations;

public record GetPendingModerationsQuery(string? Cursor, int? PageSize);
