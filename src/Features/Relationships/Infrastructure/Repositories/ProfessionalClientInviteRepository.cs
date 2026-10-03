namespace ShapeUp.Features.Relationships.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Data;
using Shared.Entities;

public class ProfessionalClientInviteRepository(RelationshipsDbContext context) : IProfessionalClientInviteRepository
{
    public async Task AddAsync(ProfessionalClientInvite invite, CancellationToken cancellationToken)
    {
        await context.Set<ProfessionalClientInvite>().AddAsync(invite, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<ProfessionalClientInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.Set<ProfessionalClientInvite>()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public Task<ProfessionalClientInvite?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Set<ProfessionalClientInvite>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> TryAcceptAsync(int inviteId, int clientUserId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var rows = await context.Set<ProfessionalClientInvite>()
            .Where(x => x.Id == inviteId
                        && x.Status == ProfessionalClientInviteStatus.Pending
                        && x.ExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(set => set
                .SetProperty(x => x.Status, ProfessionalClientInviteStatus.Accepted)
                .SetProperty(x => x.AcceptedByUserId, clientUserId)
                .SetProperty(x => x.AcceptedAtUtc, nowUtc), cancellationToken);
        return rows > 0;
    }

    public async Task<bool> ReleaseAcceptedAsync(int inviteId, int clientUserId, CancellationToken cancellationToken)
    {
        var rows = await context.Set<ProfessionalClientInvite>()
            .Where(x => x.Id == inviteId
                        && x.Status == ProfessionalClientInviteStatus.Accepted
                        && x.AcceptedByUserId == clientUserId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(x => x.Status, ProfessionalClientInviteStatus.Pending)
                .SetProperty(x => x.AcceptedByUserId, (int?)null)
                .SetProperty(x => x.AcceptedAtUtc, (DateTime?)null), cancellationToken);
        return rows > 0;
    }

    public async Task<bool> TryRevokeAsync(int inviteId, int professionalUserId, CancellationToken cancellationToken)
    {
        var rows = await context.Set<ProfessionalClientInvite>()
            .Where(x => x.Id == inviteId
                        && x.ProfessionalUserId == professionalUserId
                        && x.Status == ProfessionalClientInviteStatus.Pending)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Status, ProfessionalClientInviteStatus.Revoked), cancellationToken);
        return rows > 0;
    }

    public Task<int> CountPendingAsync(int professionalUserId, string relationshipType, DateTime nowUtc, CancellationToken cancellationToken) =>
        context.Set<ProfessionalClientInvite>()
            .CountAsync(x => x.ProfessionalUserId == professionalUserId
                             && x.RelationshipType == relationshipType
                             && x.Status == ProfessionalClientInviteStatus.Pending
                             && x.ExpiresAtUtc > nowUtc, cancellationToken);

    public async Task<IReadOnlyList<ProfessionalClientInvite>> ListPendingAsync(
        int professionalUserId, string relationshipType, DateTime nowUtc, int? afterId, int pageSize, CancellationToken cancellationToken) =>
        await context.Set<ProfessionalClientInvite>()
            .AsNoTracking()
            .Where(x => x.ProfessionalUserId == professionalUserId
                        && x.RelationshipType == relationshipType
                        && x.Status == ProfessionalClientInviteStatus.Pending
                        && x.ExpiresAtUtc > nowUtc
                        && (afterId == null || x.Id > afterId))
            .OrderBy(x => x.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task UpdateAsync(ProfessionalClientInvite invite, CancellationToken cancellationToken)
    {
        context.Set<ProfessionalClientInvite>().Update(invite);
        await context.SaveChangesAsync(cancellationToken);
    }
}
