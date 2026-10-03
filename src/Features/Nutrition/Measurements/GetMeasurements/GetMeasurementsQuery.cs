namespace ShapeUp.Features.Nutrition.Measurements.GetMeasurements;

/// <summary>
/// Both bounds are optional and inclusive; when both are given the range is at most
/// <see cref="GetMeasurementsHandler.MaxRangeDays"/> days. Newest first, paginated by cursor.
/// </summary>
public record GetMeasurementsQuery(DateOnly? From = null, DateOnly? To = null, string? Cursor = null, int? PageSize = null);
