using MongoDB.Driver;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

namespace ShapeUp.Features.Training.Shared.Abstractions;

public interface IWorkoutSessionRepository
{
    Task AddAsync(WorkoutSessionDocument session, CancellationToken cancellationToken);
    Task<WorkoutSessionDocument?> GetByIdAsync(string sessionId, CancellationToken cancellationToken);
    Task<WorkoutSessionDocument?> GetLatestCompletedByWorkoutPlanIdAsync(string workoutPlanId, CancellationToken cancellationToken);
    Task<WorkoutSessionDocument?> GetActiveByTargetUserIdAsync(int targetUserId, CancellationToken cancellationToken);
    Task UpdateStateAsync(string sessionId, DateTime savedAtUtc, List<ExecutedExerciseDocumentValueObject> exercises, CancellationToken cancellationToken);
    /// <summary>Atomically appends a set unless <paramref name="operationId"/> was already applied. Returns false when it was a duplicate.</summary>
    Task<bool> AppendSetAsync(string sessionId, string operationId, ExecutedExerciseDocumentValueObject exerciseIfMissing, ExecutedSetDocumentValueObject set, DateTime savedAtUtc, CancellationToken cancellationToken);
    Task UpdateCompletionAsync(
        string sessionId,
        DateTime endedAtUtc,
        int perceivedExertion,
        List<WorkoutPrDocumentValueObject> personalRecords,
        CancellationToken cancellationToken,
        IClientSessionHandle? mongoSession = null);
    Task CancelAsync(string sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkoutSessionDocument>> GetByTargetUserKeysetAsync(int targetUserId, DateTime? startedBeforeUtc, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkoutSessionDocument>> GetCompletedByUserInRangeAsync(int targetUserId, DateTime startInclusiveUtc, DateTime endExclusiveUtc, CancellationToken cancellationToken);
}
