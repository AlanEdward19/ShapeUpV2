namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using Dtos;
using Entities;

public interface ITrainerClientRepository
{
    Task<TrainerClient?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<TrainerClient?> GetByClientIdAsync(int clientId, CancellationToken cancellationToken);
    Task<TrainerClient?> GetByTrainerAndClientAsync(int trainerId, int clientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TrainerClient>> GetByTrainerIdKeysetAsync(int trainerId, int? lastId, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<TrainerClientWithUserDto>> GetByTrainerIdKeysetWithUserDataAsync(int trainerId, int? lastId, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(TrainerClient client, CancellationToken cancellationToken);
    Task TransferAsync(int clientId, int newTrainerId, int newPlanId, CancellationToken cancellationToken);
    Task UnassignAsync(int trainerId, int clientId, CancellationToken cancellationToken);
    Task SetPlanStatusAsync(int trainerId, int clientId, bool isActive, CancellationToken cancellationToken);
}



