namespace ShapeUp.Features.Nutrition.Measurements.GetMeasurements;

/// <summary>Both bounds are optional and inclusive.</summary>
public record GetMeasurementsQuery(DateOnly? From = null, DateOnly? To = null);
