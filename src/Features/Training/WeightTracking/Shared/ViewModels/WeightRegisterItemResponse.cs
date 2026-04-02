namespace ShapeUp.Features.Training.WeightTracking.Shared.ViewModels;

public record WeightRegisterItemResponse(DateOnly Date, decimal Weight, DateTime UpdatedAtUtc);
