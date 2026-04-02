namespace ShapeUp.Features.GymManagement.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Shared.Abstractions;
using Shared.Dtos;
using Shared.Entities;
using ShapeUp.Features.Authorization.Shared.Data;

public class TrainerClientRepository(GymManagementDbContext context, AuthorizationDbContext authContext) : ITrainerClientRepository
{
    public async Task<TrainerClient?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await context.TrainerClients
            .Include(c => c.TrainerPlan)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<TrainerClient?> GetByClientIdAsync(int clientId, CancellationToken cancellationToken) =>
        await context.TrainerClients
            .Include(c => c.TrainerPlan)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);

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

    public async Task<IReadOnlyList<TrainerClientWithUserDto>> GetByTrainerIdKeysetWithUserDataAsync(
        int trainerId, 
        int? lastId, 
        int pageSize, 
        CancellationToken cancellationToken)
    {
        var query = context.TrainerClients
            .AsNoTracking()
            .Where(c => c.TrainerId == trainerId)
            .Select(c => new
            {
                TrainerClient = c,
                TrainerPlan = c.TrainerPlan
            });

        if (lastId.HasValue) 
            query = query.Where(x => x.TrainerClient.Id < lastId.Value);

        var trainersClientData = await query
            .OrderByDescending(x => x.TrainerClient.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var clientIds = trainersClientData.Select(x => x.TrainerClient.ClientId).ToList();
        var users = await authContext.Users
            .AsNoTracking()
            .Where(u => clientIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var result = trainersClientData.Select(x => new TrainerClientWithUserDto
        {
            Id = x.TrainerClient.Id,
            TrainerId = x.TrainerClient.TrainerId,
            ClientId = x.TrainerClient.ClientId,
            ClientName = users.TryGetValue(x.TrainerClient.ClientId, out var user) 
                ? (user.DisplayName ?? user.Email)
                : "Unknown",
            ClientEmail = users.TryGetValue(x.TrainerClient.ClientId, out var u) 
                ? u.Email
                : "unknown@email.com",
            TrainerPlanId = x.TrainerClient.TrainerPlanId,
            PlanName = x.TrainerPlan?.Name,
            IsActive = x.TrainerClient.IsActive,
            EnrolledAt = x.TrainerClient.EnrolledAt
        }).ToList();

        return result;
    }


    public async Task AddAsync(TrainerClient client, CancellationToken cancellationToken)
    {
        await context.TrainerClients.AddAsync(client, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task TransferAsync(int clientId, int newTrainerId, int newPlanId, CancellationToken cancellationToken) =>
        await context.TrainerClients
            .Where(c => c.Id == clientId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.TrainerId, newTrainerId)
                .SetProperty(c => c.TrainerPlanId, newPlanId),
                cancellationToken);

    public async Task UnassignAsync(int trainerId, int clientId, CancellationToken cancellationToken) =>
        await context.TrainerClients
            .Where(c => c.TrainerId == trainerId && c.ClientId == clientId)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task SetPlanStatusAsync(int trainerId, int clientId, bool isActive, CancellationToken cancellationToken) =>
        await context.TrainerClients
            .Where(c => c.TrainerId == trainerId && c.ClientId == clientId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, isActive), cancellationToken);
}

