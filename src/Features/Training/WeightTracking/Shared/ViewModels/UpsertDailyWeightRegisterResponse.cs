namespace ShapeUp.Features.Training.WeightTracking.Shared.ViewModels;

public record UpsertDailyWeightRegisterResponse(DateOnly Date, decimal Weight, decimal? TargetWeight, DateTime UpdatedAtUtc);
