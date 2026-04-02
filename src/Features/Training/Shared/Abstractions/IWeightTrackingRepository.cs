using ShapeUp.Features.Training.Shared.Documents;

namespace ShapeUp.Features.Training.Shared.Abstractions;

public interface IWeightTrackingRepository
{
    Task UpsertTargetWeightAsync(int userId, decimal targetWeight, DateTime updatedAtUtc, CancellationToken cancellationToken);
    Task<WeightTargetDocument?> GetTargetByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task UpsertDailyWeightAsync(int userId, decimal weight, DateOnly day, DateTime updatedAtUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<WeightRegisterDocument>> GetRegistersByRangeAsync(int userId, DateOnly startDate, DateOnly endDateInclusive, CancellationToken cancellationToken);
}
