namespace ShapeUp.Features.GymManagement.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Shared.Abstractions;
using Shared.Entities;

public class GymPlanRepository(GymManagementDbContext context) : IGymPlanRepository
{
    public async Task<GymPlan?> GetByIdAsync(int planId, CancellationToken cancellationToken) =>
        await context.GymPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

    public async Task<IReadOnlyList<GymPlan>> GetByGymIdKeysetAsync(int gymId, int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.GymPlans.AsNoTracking().Where(p => p.GymId == gymId);
        if (lastId.HasValue) query = query.Where(p => p.Id < lastId.Value);
        return await query.OrderByDescending(p => p.Id).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GymPlan plan, CancellationToken cancellationToken)
    {
        context.GymPlans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(GymPlan plan, CancellationToken cancellationToken)
    {
        plan.UpdatedAt = DateTime.UtcNow;
        context.GymPlans.Update(plan);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int planId, CancellationToken cancellationToken) =>
        await context.GymPlans.Where(p => p.Id == planId).ExecuteDeleteAsync(cancellationToken);
}

