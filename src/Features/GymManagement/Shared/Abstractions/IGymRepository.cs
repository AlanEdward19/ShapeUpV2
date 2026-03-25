namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using ShapeUp.Features.GymManagement.Shared.Entities;

public interface IGymRepository
{
    Task<Gym?> GetByIdAsync(int gymId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Gym>> GetAllKeysetAsync(int? lastId, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<Gym>> GetByOwnerIdKeysetAsync(int ownerId, int? lastId, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(Gym gym, CancellationToken cancellationToken);
    Task UpdateAsync(Gym gym, CancellationToken cancellationToken);
}

