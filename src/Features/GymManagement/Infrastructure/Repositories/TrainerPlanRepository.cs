namespace ShapeUp.Features.GymManagement.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Shared.Abstractions;
using Shared.Entities;

public class TrainerPlanRepository(GymManagementDbContext context) : ITrainerPlanRepository
{
    public async Task<TrainerPlan?> GetByIdAsync(int planId, CancellationToken cancellationToken) =>
        await context.TrainerPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

    public async Task<IReadOnlyList<TrainerPlan>> GetByTrainerIdKeysetAsync(int trainerId, int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.TrainerPlans.AsNoTracking().Where(p => p.TrainerId == trainerId);
        if (lastId.HasValue) query = query.Where(p => p.Id < lastId.Value);
        return await query.OrderByDescending(p => p.Id).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TrainerPlan plan, CancellationToken cancellationToken)
    {
        context.TrainerPlans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TrainerPlan plan, CancellationToken cancellationToken)
    {
        plan.UpdatedAt = DateTime.UtcNow;
        context.TrainerPlans.Update(plan);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int planId, CancellationToken cancellationToken) =>
        await context.TrainerPlans.Where(p => p.Id == planId).ExecuteDeleteAsync(cancellationToken);
}

