using ShapeUp.Features.Training.Shared.Entities;

namespace ShapeUp.Features.Training.Shared.Abstractions;

public interface IExerciseEquivalentRepository
{
    Task<IReadOnlyList<Exercise>> GetEquivalentsAsync(int exerciseId, CancellationToken cancellationToken);
    Task SetEquivalentAsync(int exerciseId, int otherExerciseId, CancellationToken cancellationToken);
    Task RemoveEquivalentAsync(int exerciseId, int otherExerciseId, CancellationToken cancellationToken);
}
