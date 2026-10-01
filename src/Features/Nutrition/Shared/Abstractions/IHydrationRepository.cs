using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

public interface IHydrationRepository
{
    Task<HydrationDayDocument?> GetDayAsync(int userId, DateOnly day, CancellationToken cancellationToken);
    Task<IReadOnlyList<HydrationDayDocument>> GetRangeAsync(int userId, DateOnly startDate, DateOnly endDateInclusive, CancellationToken cancellationToken);
    /// <summary>Sets the absolute total of the day (creates the record when missing).</summary>
    Task UpsertDayAsync(int userId, DateOnly day, int totalMl, DateTime updatedAtUtc, CancellationToken cancellationToken);
}
