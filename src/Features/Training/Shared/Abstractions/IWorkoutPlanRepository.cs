using ShapeUp.Features.Training.Shared.Documents;

namespace ShapeUp.Features.Training.Shared.Abstractions;

public interface IWorkoutPlanRepository
{
    Task AddAsync(WorkoutPlanDocument plan, CancellationToken cancellationToken);
    Task<WorkoutPlanDocument?> GetByIdAsync(string planId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkoutPlanDocument>> GetByTargetUserKeysetAsync(int targetUserId, DateTime? createdBeforeUtc, int pageSize, CancellationToken cancellationToken);
}

