using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

namespace ShapeUp.Features.Training.Shared.Abstractions;

public interface IWorkoutSessionRepository
{
    Task AddAsync(WorkoutSessionDocument session, CancellationToken cancellationToken);
    Task<WorkoutSessionDocument?> GetByIdAsync(string sessionId, CancellationToken cancellationToken);
    Task UpdateStateAsync(string sessionId, DateTime savedAtUtc, List<ExecutedExerciseDocumentValueObject> exercises, CancellationToken cancellationToken);
    Task UpdateCompletionAsync(string sessionId, DateTime endedAtUtc, int perceivedExertion, List<WorkoutPrDocumentValueObject> personalRecords, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkoutSessionDocument>> GetByTargetUserKeysetAsync(int targetUserId, DateTime? startedBeforeUtc, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkoutSessionDocument>> GetCompletedByUserInRangeAsync(int targetUserId, DateTime startInclusiveUtc, DateTime endExclusiveUtc, CancellationToken cancellationToken);
}
