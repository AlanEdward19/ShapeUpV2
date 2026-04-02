namespace ShapeUp.Features.Training.WeightTracking.Shared.ViewModels;

public record GetWeightRegistersResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? TargetWeight,
    IReadOnlyList<WeightRegisterItemResponse> Items);
