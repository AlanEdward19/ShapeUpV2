namespace ShapeUp.Features.Relationships.Shared.Abstractions;

using Entities;

public interface IProfessionalClientInviteRepository
{
    Task AddAsync(ProfessionalClientInvite invite, CancellationToken cancellationToken);

    Task<ProfessionalClientInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<ProfessionalClientInvite?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Single-use claim: conditional UPDATE (<c>WHERE Status = Pending AND ExpiresAtUtc &gt; now</c>) that marks the invite
    /// Accepted. Returns false for every caller but the first, so two concurrent accepts cannot both win.
    /// </summary>
    Task<bool> TryAcceptAsync(int inviteId, int clientUserId, DateTime nowUtc, CancellationToken cancellationToken);

    /// <summary>Undoes <see cref="TryAcceptAsync"/> (back to Pending) when what came after it failed.</summary>
    Task<bool> ReleaseAcceptedAsync(int inviteId, int clientUserId, CancellationToken cancellationToken);

    /// <summary>Revokes a Pending invite of the professional. Returns false if it is not theirs or not pending anymore.</summary>
    Task<bool> TryRevokeAsync(int inviteId, int professionalUserId, CancellationToken cancellationToken);

    /// <summary>Pending, not yet expired invites of the professional for the type.</summary>
    Task<int> CountPendingAsync(int professionalUserId, string relationshipType, DateTime nowUtc, CancellationToken cancellationToken);

    /// <summary>Keyset page (ascending Id) of the pending, not yet expired invites.</summary>
    Task<IReadOnlyList<ProfessionalClientInvite>> ListPendingAsync(
        int professionalUserId, string relationshipType, DateTime nowUtc, int? afterId, int pageSize, CancellationToken cancellationToken);

    Task UpdateAsync(ProfessionalClientInvite invite, CancellationToken cancellationToken);
}
