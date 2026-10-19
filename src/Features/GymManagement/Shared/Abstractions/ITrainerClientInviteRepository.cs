namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using Entities;

public interface ITrainerClientInviteRepository
{
    Task<TrainerClientInvite?> GetByIdAsync(int inviteId, CancellationToken cancellationToken);
    Task<TrainerClientInvite?> GetByTokenHashAsync(string accessTokenHash, CancellationToken cancellationToken);
    Task<TrainerClientInvite?> GetActiveByTrainerAndEmailAsync(int trainerId, string inviteeEmail, CancellationToken cancellationToken);
    /// <summary>The trainer's invites in any status, newest first (Id keyset: only Ids below <paramref name="beforeId"/>).</summary>
    Task<IReadOnlyList<TrainerClientInvite>> ListAsync(int trainerId, int? beforeId, int limit, CancellationToken cancellationToken);

    /// <summary>Invites still waiting (Invited and not past <paramref name="nowUtc"/>), optionally ignoring one that is about to be superseded.</summary>
    Task<int> CountWaitingAsync(int trainerId, DateTime nowUtc, int? exceptInviteId, CancellationToken cancellationToken);
    /// <summary>
    /// Single-use claim: conditional UPDATE (<c>WHERE Status = Invited AND ExpiresAtUtc &gt; now</c>) that marks the invite
    /// Accepted. False for every caller but the first, and false once the trainer revoked it.
    /// </summary>
    Task<bool> TryAcceptAsync(int inviteId, int clientUserId, DateTime nowUtc, CancellationToken cancellationToken);

    /// <summary>Undoes <see cref="TryAcceptAsync"/> (back to Invited) when what came after it failed.</summary>
    Task<bool> ReleaseAcceptedAsync(int inviteId, int clientUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes an invite still waiting: conditional UPDATE (<c>WHERE TrainerId = trainer AND Status = Invited AND ExpiresAtUtc &gt; now</c>).
    /// False if it is not the trainer's, was accepted meanwhile, expired or was already revoked.
    /// </summary>
    Task<bool> TryRevokeAsync(int inviteId, int trainerId, DateTime nowUtc, CancellationToken cancellationToken);

    Task AddAsync(TrainerClientInvite invite, CancellationToken cancellationToken);
    Task UpdateAsync(TrainerClientInvite invite, CancellationToken cancellationToken);
}

