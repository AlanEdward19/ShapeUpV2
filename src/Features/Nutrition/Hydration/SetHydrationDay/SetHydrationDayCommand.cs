namespace ShapeUp.Features.Nutrition.Hydration.SetHydrationDay;

/// <param name="TotalMl">Absolute total of the day, not an increment, so a retry is safe.</param>
/// <param name="ClientUpdatedAtUtc">When the client registered this total; a write older than the stored one is ignored (last write wins).</param>
public record SetHydrationDayCommand(DateOnly Date, int TotalMl, DateTime? ClientUpdatedAtUtc = null);
