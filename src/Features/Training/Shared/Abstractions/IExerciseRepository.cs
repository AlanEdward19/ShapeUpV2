using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Shared.Abstractions;

public interface IExerciseRepository
{
    Task<Exercise?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Exercise>> GetKeysetPageAsync(int? lastId, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<Exercise>> SuggestAsync(string name, MuscleGroup[] muscleGroups, int[] equipmentIds, int limit, CancellationToken cancellationToken);
    Task AddAsync(Exercise exercise, CancellationToken cancellationToken);
    Task UpdateAsync(Exercise exercise, CancellationToken cancellationToken);
    Task DeleteAsync(Exercise exercise, CancellationToken cancellationToken);
}
