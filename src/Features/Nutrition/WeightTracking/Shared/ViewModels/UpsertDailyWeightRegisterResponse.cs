namespace ShapeUp.Features.Nutrition.WeightTracking.Shared.ViewModels;

public record UpsertDailyWeightRegisterResponse(DateOnly Date, decimal Weight, decimal? TargetWeight, DateTime UpdatedAtUtc);
