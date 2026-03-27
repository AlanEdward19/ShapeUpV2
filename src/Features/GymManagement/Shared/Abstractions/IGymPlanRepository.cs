namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using Entities;

public interface IGymPlanRepository
{
    Task<GymPlan?> GetByIdAsync(int planId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GymPlan>> GetByGymIdKeysetAsync(int gymId, int? lastId, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(GymPlan plan, CancellationToken cancellationToken);
    Task UpdateAsync(GymPlan plan, CancellationToken cancellationToken);
    Task DeleteAsync(int planId, CancellationToken cancellationToken);
}

