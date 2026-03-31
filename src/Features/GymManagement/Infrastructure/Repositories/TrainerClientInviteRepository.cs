namespace ShapeUp.Features.GymManagement.Infrastructure.Repositories;

using Data;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Entities;

public class TrainerClientInviteRepository(GymManagementDbContext context) : ITrainerClientInviteRepository
{
    public async Task<TrainerClientInvite?> GetByTokenHashAsync(string accessTokenHash, CancellationToken cancellationToken) =>
        await context.TrainerClientInvites
            .AsNoTracking()
            .FirstOrDefaultAsync(invite => invite.AccessTokenHash == accessTokenHash, cancellationToken);

    public async Task<TrainerClientInvite?> GetActiveByTrainerAndEmailAsync(int trainerId, string inviteeEmail, CancellationToken cancellationToken) =>
        await context.TrainerClientInvites
            .AsNoTracking()
            .FirstOrDefaultAsync(invite =>
                invite.TrainerId == trainerId &&
                invite.InviteeEmail == inviteeEmail &&
                invite.Status == TrainerClientInviteStatus.Invited,
                cancellationToken);

    public async Task AddAsync(TrainerClientInvite invite, CancellationToken cancellationToken)
    {
        await context.TrainerClientInvites.AddAsync(invite, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TrainerClientInvite invite, CancellationToken cancellationToken)
    {
        context.TrainerClientInvites.Update(invite);
        await context.SaveChangesAsync(cancellationToken);
    }
}

