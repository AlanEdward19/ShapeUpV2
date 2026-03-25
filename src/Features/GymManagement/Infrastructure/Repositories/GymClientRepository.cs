namespace ShapeUp.Features.GymManagement.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

public class GymClientRepository(GymManagementDbContext context) : IGymClientRepository
{
    public async Task<GymClient?> GetByIdAsync(int clientId, CancellationToken cancellationToken) =>
        await context.GymClients
            .Include(c => c.GymPlan)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

    public async Task<GymClient?> GetByGymAndUserAsync(int gymId, int userId, CancellationToken cancellationToken) =>
        await context.GymClients
            .Include(c => c.GymPlan)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.GymId == gymId && c.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<GymClient>> GetByGymIdKeysetAsync(int gymId, int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.GymClients.Include(c => c.GymPlan).AsNoTracking().Where(c => c.GymId == gymId);
        if (lastId.HasValue) query = query.Where(c => c.Id < lastId.Value);
        return await query.OrderByDescending(c => c.Id).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GymClient>> GetByTrainerIdKeysetAsync(int gymId, int trainerId, int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.GymClients.Include(c => c.GymPlan).AsNoTracking()
            .Where(c => c.GymId == gymId && c.TrainerId == trainerId);
        if (lastId.HasValue) query = query.Where(c => c.Id < lastId.Value);
        return await query.OrderByDescending(c => c.Id).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GymClient client, CancellationToken cancellationToken)
    {
        context.GymClients.Add(client);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignTrainerAsync(int clientId, int? trainerId, CancellationToken cancellationToken) =>
        await context.GymClients
            .Where(c => c.Id == clientId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.TrainerId, trainerId), cancellationToken);
}

