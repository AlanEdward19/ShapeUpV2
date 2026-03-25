namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using ShapeUp.Features.GymManagement.Shared.Entities;

public interface ITrainerClientRepository
{
    Task<TrainerClient?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<TrainerClient?> GetByTrainerAndClientAsync(int trainerId, int clientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TrainerClient>> GetByTrainerIdKeysetAsync(int trainerId, int? lastId, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(TrainerClient client, CancellationToken cancellationToken);
    Task TransferAsync(int clientId, int newTrainerId, int newPlanId, CancellationToken cancellationToken);
}

