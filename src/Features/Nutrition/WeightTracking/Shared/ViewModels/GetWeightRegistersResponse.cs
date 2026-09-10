namespace ShapeUp.Features.Nutrition.WeightTracking.Shared.ViewModels;

public record GetWeightRegistersResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? TargetWeight,
    IReadOnlyList<WeightRegisterItemResponse> Items);
