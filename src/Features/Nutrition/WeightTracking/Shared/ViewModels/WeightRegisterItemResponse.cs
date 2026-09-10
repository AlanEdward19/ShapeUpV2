namespace ShapeUp.Features.Nutrition.WeightTracking.Shared.ViewModels;

public record WeightRegisterItemResponse(DateOnly Date, decimal Weight, DateTime UpdatedAtUtc);
