namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using ShapeUp.Features.GymManagement.Shared.Entities;

public interface ITrainerPlanRepository
{
    Task<TrainerPlan?> GetByIdAsync(int planId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TrainerPlan>> GetByTrainerIdKeysetAsync(int trainerId, int? lastId, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(TrainerPlan plan, CancellationToken cancellationToken);
    Task UpdateAsync(TrainerPlan plan, CancellationToken cancellationToken);
    Task DeleteAsync(int planId, CancellationToken cancellationToken);
}

