namespace ShapeUp.Features.GymManagement.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Shared.Abstractions;
using Shared.Entities;

public class GymRepository(GymManagementDbContext context) : IGymRepository
{
    public async Task<Gym?> GetByIdAsync(int gymId, CancellationToken cancellationToken) =>
        await context.Gyms
            .Include(g => g.PlatformTier)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

    public async Task<IReadOnlyList<Gym>> GetAllKeysetAsync(int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.Gyms.Include(g => g.PlatformTier).AsNoTracking().AsQueryable();
        if (lastId.HasValue) query = query.Where(g => g.Id < lastId.Value);
        return await query.OrderByDescending(g => g.Id).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Gym>> GetByOwnerIdKeysetAsync(int ownerId, int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.Gyms.Include(g => g.PlatformTier).AsNoTracking().Where(g => g.OwnerId == ownerId);
        if (lastId.HasValue) query = query.Where(g => g.Id < lastId.Value);
        return await query.OrderByDescending(g => g.Id).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Gym gym, CancellationToken cancellationToken)
    {
        context.Gyms.Add(gym);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Gym gym, CancellationToken cancellationToken)
    {
        gym.UpdatedAt = DateTime.UtcNow;
        context.Gyms.Update(gym);
        await context.SaveChangesAsync(cancellationToken);
    }
}

