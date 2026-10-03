namespace ShapeUp.Features.Nutrition.Measurements.AddMeasurement;

/// <summary>At least one of the measures is required.</summary>
public record AddMeasurementCommand(
    DateOnly Date,
    decimal? WeightKg = null,
    decimal? HeightCm = null,
    decimal? BodyFatPercent = null,
    decimal? WaistCm = null,
    decimal? HipCm = null,
    string? Notes = null);
