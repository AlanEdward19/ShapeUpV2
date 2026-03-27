using ShapeUp.Features.Training.Shared.Entities;

namespace ShapeUp.Features.Training.Shared.Abstractions;

public interface IMuscleRepository
{
    Task<Muscle?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Muscle>> GetKeysetPageAsync(int? lastId, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(Muscle muscle, CancellationToken cancellationToken);
    Task UpdateAsync(Muscle muscle, CancellationToken cancellationToken);
    Task DeleteAsync(Muscle muscle, CancellationToken cancellationToken);
}

