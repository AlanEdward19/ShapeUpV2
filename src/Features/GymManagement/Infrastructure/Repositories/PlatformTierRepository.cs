namespace ShapeUp.Features.GymManagement.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Shared.Abstractions;
using Shared.Entities;

public class PlatformTierRepository(GymManagementDbContext context) : IPlatformTierRepository
{
    public async Task<PlatformTier?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await context.PlatformTiers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<PlatformTier?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        await context.PlatformTiers.AsNoTracking().FirstOrDefaultAsync(p => p.Name == name, cancellationToken);

    public async Task<IReadOnlyList<PlatformTier>> GetAllKeysetAsync(int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.PlatformTiers.AsNoTracking().AsQueryable();
        if (lastId.HasValue) query = query.Where(p => p.Id < lastId.Value);
        return await query.OrderByDescending(p => p.Id).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PlatformTier tier, CancellationToken cancellationToken)
    {
        context.PlatformTiers.Add(tier);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PlatformTier tier, CancellationToken cancellationToken)
    {
        tier.UpdatedAt = DateTime.UtcNow;
        context.PlatformTiers.Update(tier);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken) =>
        await context.PlatformTiers.Where(p => p.Id == id).ExecuteDeleteAsync(cancellationToken);
}

