using ShapeUp.Features.Training.Shared.Entities;

namespace ShapeUp.Features.Training.Shared.Abstractions;

public interface IExerciseRequestRepository
{
    Task AddAsync(ExerciseRequest request, CancellationToken cancellationToken);

    /// <summary>Tracked, so a review handler can change it and call <see cref="SaveAsync"/>.</summary>
    Task<ExerciseRequest?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CountPendingByUserAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Newest first (Id descending); <paramref name="userId"/> and <paramref name="status"/> are optional filters.</summary>
    Task<IReadOnlyList<ExerciseRequest>> GetKeysetPageAsync(
        int? userId, string? status, int? lastId, int pageSize, CancellationToken cancellationToken);

    /// <summary>Saves the tracked request together with an optional new exercise in a single transaction.</summary>
    Task SaveAsync(Exercise? newExercise, CancellationToken cancellationToken);
}
