using ShapeUp.Features.Nutrition.Measurements.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Entities;

namespace ShapeUp.Features.Nutrition.Measurements.Shared;

internal static class MeasurementMapper
{
    internal static MeasurementResponse ToResponse(NutritionMeasurement m) =>
        new(m.Id, m.Date, m.WeightKg, m.HeightCm, m.BodyFatPercent, m.WaistCm, m.HipCm, m.Notes, m.RecordedByUserId, m.CreatedAtUtc);
}
