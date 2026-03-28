using ShapeUp.Features.Training.Shared.Entities;

namespace ShapeUp.Features.Training.Shared.Abstractions;

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Equipment>> GetKeysetPageAsync(int? lastId, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(Equipment equipment, CancellationToken cancellationToken);
    Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken);
    Task DeleteAsync(Equipment equipment, CancellationToken cancellationToken);
}

