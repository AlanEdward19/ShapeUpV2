namespace ShapeUp.Features.GymManagement.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

public class TrainerClientRepository(GymManagementDbContext context) : ITrainerClientRepository
{
    public async Task<TrainerClient?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await context.TrainerClients
            .Include(c => c.TrainerPlan)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<TrainerClient?> GetByTrainerAndClientAsync(int trainerId, int clientId, CancellationToken cancellationToken) =>
        await context.TrainerClients
            .Include(c => c.TrainerPlan)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TrainerId == trainerId && c.ClientId == clientId, cancellationToken);

    public async Task<IReadOnlyList<TrainerClient>> GetByTrainerIdKeysetAsync(int trainerId, int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.TrainerClients.Include(c => c.TrainerPlan).AsNoTracking().Where(c => c.TrainerId == trainerId);
        if (lastId.HasValue) query = query.Where(c => c.Id < lastId.Value);
        return await query.OrderByDescending(c => c.Id).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TrainerClient client, CancellationToken cancellationToken)
    {
        context.TrainerClients.Add(client);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task TransferAsync(int clientId, int newTrainerId, int newPlanId, CancellationToken cancellationToken) =>
        await context.TrainerClients
            .Where(c => c.Id == clientId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.TrainerId, newTrainerId)
                .SetProperty(c => c.TrainerPlanId, newPlanId),
                cancellationToken);
}

