namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using Entities;

public interface IGymClientRepository
{
    Task<GymClient?> GetByIdAsync(int clientId, CancellationToken cancellationToken);
    Task<GymClient?> GetByGymAndUserAsync(int gymId, int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GymClient>> GetByGymIdKeysetAsync(int gymId, int? lastId, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<GymClient>> GetByTrainerIdKeysetAsync(int gymId, int trainerId, int? lastId, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(GymClient client, CancellationToken cancellationToken);
    Task AssignTrainerAsync(int clientId, int? trainerId, CancellationToken cancellationToken);
}

