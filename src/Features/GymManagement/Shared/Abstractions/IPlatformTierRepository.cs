namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using ShapeUp.Features.GymManagement.Shared.Entities;

public interface IPlatformTierRepository
{
    Task<PlatformTier?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<PlatformTier?> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlatformTier>> GetAllKeysetAsync(int? lastId, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(PlatformTier tier, CancellationToken cancellationToken);
    Task UpdateAsync(PlatformTier tier, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

