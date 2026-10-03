namespace ShapeUp.Features.Nutrition.Measurements.Shared.ViewModels;

public record MeasurementResponse(
    int Id,
    DateOnly Date,
    decimal? WeightKg,
    decimal? HeightCm,
    decimal? BodyFatPercent,
    decimal? WaistCm,
    decimal? HipCm,
    string? Notes,
    int RecordedByUserId,
    DateTime CreatedAtUtc);
