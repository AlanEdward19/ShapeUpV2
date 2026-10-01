namespace ShapeUp.Features.Nutrition.Hydration.Shared;

/// <summary>Water consumed on a day. <see cref="UpdatedAtUtc"/> is null (and <see cref="TotalMl"/> 0) when nothing was recorded.</summary>
public record HydrationDayResponse(DateOnly Date, int TotalMl, DateTime? UpdatedAtUtc);

public record HydrationRangeResponse(DateOnly From, DateOnly To, HydrationDayResponse[] Days);
