namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using ShapeUp.Features.GymManagement.Shared.Entities;

public interface IGymStaffRepository
{
    Task<GymStaff?> GetByIdAsync(int staffId, CancellationToken cancellationToken);
    Task<GymStaff?> GetByGymAndUserAsync(int gymId, int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GymStaff>> GetByGymIdKeysetAsync(int gymId, int? lastId, int pageSize, CancellationToken cancellationToken);
    Task<bool> IsStaffAsync(int gymId, int userId, CancellationToken cancellationToken);
    Task<bool> IsOwnerOrReceptionistAsync(int gymId, int userId, int ownerId, CancellationToken cancellationToken);
    Task AddAsync(GymStaff staff, CancellationToken cancellationToken);
    Task RemoveAsync(int staffId, CancellationToken cancellationToken);
}

