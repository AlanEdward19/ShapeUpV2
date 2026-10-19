namespace ShapeUp.Features.GymManagement.Infrastructure.Repositories;

using Data;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Entities;

public class TrainerClientInviteRepository(GymManagementDbContext context) : ITrainerClientInviteRepository
{
    public async Task<TrainerClientInvite?> GetByIdAsync(int inviteId, CancellationToken cancellationToken) =>
        await context.TrainerClientInvites
            .AsNoTracking()
            .FirstOrDefaultAsync(invite => invite.Id == inviteId, cancellationToken);

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

    public async Task<IReadOnlyList<TrainerClientInvite>> ListAsync(int trainerId, int? beforeId, int limit, CancellationToken cancellationToken) =>
        await context.TrainerClientInvites
            .AsNoTracking()
            .Where(invite => invite.TrainerId == trainerId && (beforeId == null || invite.Id < beforeId))
            .OrderByDescending(invite => invite.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<int> CountWaitingAsync(int trainerId, DateTime nowUtc, int? exceptInviteId, CancellationToken cancellationToken) =>
        await context.TrainerClientInvites
            .CountAsync(invite => invite.TrainerId == trainerId
                && invite.Status == TrainerClientInviteStatus.Invited
                && invite.ExpiresAtUtc > nowUtc
                && (exceptInviteId == null || invite.Id != exceptInviteId),
                cancellationToken);

    public async Task<bool> TryAcceptAsync(int inviteId, int clientUserId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var rows = await context.TrainerClientInvites
            .Where(invite => invite.Id == inviteId
                             && invite.Status == TrainerClientInviteStatus.Invited
                             && invite.ExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(set => set
                .SetProperty(invite => invite.Status, TrainerClientInviteStatus.Accepted)
                .SetProperty(invite => invite.AcceptedByUserId, clientUserId)
                .SetProperty(invite => invite.AcceptedAtUtc, nowUtc), cancellationToken);
        return rows > 0;
    }

    public async Task<bool> ReleaseAcceptedAsync(int inviteId, int clientUserId, CancellationToken cancellationToken)
    {
        var rows = await context.TrainerClientInvites
            .Where(invite => invite.Id == inviteId
                             && invite.Status == TrainerClientInviteStatus.Accepted
                             && invite.AcceptedByUserId == clientUserId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(invite => invite.Status, TrainerClientInviteStatus.Invited)
                .SetProperty(invite => invite.AcceptedByUserId, (int?)null)
                .SetProperty(invite => invite.AcceptedAtUtc, (DateTime?)null), cancellationToken);
        return rows > 0;
    }

    public async Task<bool> TryRevokeAsync(int inviteId, int trainerId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var rows = await context.TrainerClientInvites
            .Where(invite => invite.Id == inviteId
                             && invite.TrainerId == trainerId
                             && invite.Status == TrainerClientInviteStatus.Invited
                             && invite.ExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(set => set.SetProperty(invite => invite.Status, TrainerClientInviteStatus.Revoked), cancellationToken);
        return rows > 0;
    }

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

