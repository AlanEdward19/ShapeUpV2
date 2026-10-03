namespace ShapeUp.Features.Credentials.Shared.Lifecycle;

using Abstractions;
using Entities;
using Errors;
using Microsoft.Extensions.Logging;
using StateMachine;
using ShapeUp.Shared.Results;

/// <summary>
/// Changes a credential's status and keeps the platform role in step.
/// The credential row and the role live in different DbContexts, so they cannot share one transaction.
/// Instead the status change is the atomic step (rowversion check; the loser gets a 409), the role is then
/// synchronised with a few idempotent retries, and if that still fails the status change is undone with a
/// conditional UPDATE, so a credential is never left Verified without its role (or ended with the role kept).
/// </summary>
public class CredentialLifecycle(
    IProfessionalCredentialRepository repository,
    IProfessionalRoleGranter roleGranter,
    ILogger<CredentialLifecycle>? logger = null) : ICredentialLifecycle
{
    /// <summary>How long a verification stays valid when the reviewer gives no date; after that the professional must be reverified.</summary>
    public const int DefaultValidityMonths = 12;

    private static readonly TimeSpan[] RoleSyncRetryDelays = [TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(150)];

    public async Task<Result> SendToReviewAsync(ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        if (!CredentialStatusGuard.IsValidTransition(credential.Status, CredentialStatus.UnderReview))
            return Invalid(credential, CredentialStatus.UnderReview);

        credential.Status = CredentialStatus.UnderReview;
        return await SaveAsync(credential, cancellationToken);
    }

    public async Task<Result> ApproveAsync(
        ProfessionalCredential credential, int? reviewerUserId, CancellationToken cancellationToken, DateTime? expiresAtUtc = null)
    {
        if (!CredentialStatusGuard.IsValidTransition(credential.Status, CredentialStatus.Verified))
            return Invalid(credential, CredentialStatus.Verified);

        var before = credential.Snapshot();
        var now = DateTime.UtcNow;
        credential.Status = CredentialStatus.Verified;
        credential.VerifiedAt = now;
        credential.ExpiresAt = expiresAtUtc ?? now.AddMonths(DefaultValidityMonths);
        credential.ReviewedAt = now;
        credential.ReviewedByUserId = reviewerUserId;
        credential.RejectionReason = null;

        var saved = await SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            Restore(credential, before);
            return saved;
        }

        if (await SyncRoleAsync(() => roleGranter.GrantAsync(credential, cancellationToken), credential, cancellationToken))
            return Result.Success();

        await RevertAsync(before, CredentialStatus.Verified);
        Restore(credential, before);
        return Result.Failure(CredentialErrors.RoleSyncFailed());
    }

    public async Task<Result> RejectAsync(ProfessionalCredential credential, string reason, int? reviewerUserId, CancellationToken cancellationToken)
    {
        if (!CredentialStatusGuard.IsValidTransition(credential.Status, CredentialStatus.Rejected))
            return Invalid(credential, CredentialStatus.Rejected);

        var before = credential.Snapshot();
        credential.Status = CredentialStatus.Rejected;
        credential.ReviewedAt = DateTime.UtcNow;
        credential.ReviewedByUserId = reviewerUserId;
        credential.RejectionReason = reason;

        var saved = await SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
            Restore(credential, before);
        return saved;
    }

    public async Task<Result> EndAsync(
        ProfessionalCredential credential,
        CredentialStatus target,
        DateTime nowUtc,
        CancellationToken cancellationToken,
        string? reason = null,
        int? endedByUserId = null)
    {
        if (!CredentialStatusGuard.LosesProfessionalAccess(target)
            || !CredentialStatusGuard.IsValidTransition(credential.Status, target))
            return Invalid(credential, target);

        var before = credential.Snapshot();
        credential.Status = target;
        credential.EndedAt = nowUtc;
        credential.EndedByUserId = endedByUserId;
        credential.EndReason = reason;

        var saved = await SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            Restore(credential, before);
            return saved;
        }

        if (await SyncRoleAsync(() => roleGranter.ReleaseAsync(credential, nowUtc, cancellationToken), credential, cancellationToken))
            return Result.Success();

        await RevertAsync(before, target);
        Restore(credential, before);
        return Result.Failure(CredentialErrors.RoleSyncFailed());
    }

    private async Task<Result> SaveAsync(ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        try
        {
            await repository.UpdateAsync(credential, cancellationToken);
            return Result.Success();
        }
        catch (CredentialConflictException ex)
        {
            return Result.Failure(ex.ToError(credential.ProfessionType));
        }
    }

    /// <summary>Runs an idempotent role step, retrying a couple of times on transient failures.</summary>
    private async Task<bool> SyncRoleAsync(Func<Task> step, ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await step();
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The caller gave up: no retries, and the caller undoes the status change.
                return false;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Role sync for credential {CredentialId} failed (attempt {Attempt})", credential.Id, attempt + 1);
                if (attempt >= RoleSyncRetryDelays.Length)
                    return false;
                await Task.Delay(RoleSyncRetryDelays[attempt], CancellationToken.None);
            }
        }
    }

    private async Task RevertAsync(ProfessionalCredential before, CredentialStatus expectedCurrent)
    {
        try
        {
            // CancellationToken.None on purpose: the undo must run even if the request was aborted.
            var reverted = await repository.RevertAsync(before, expectedCurrent, CancellationToken.None);
            if (!reverted)
                logger?.LogWarning("Credential {CredentialId} was changed by someone else before the status change could be undone", before.Id);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Could not undo the status change of credential {CredentialId}", before.Id);
        }
    }

    private static void Restore(ProfessionalCredential credential, ProfessionalCredential before)
    {
        credential.Status = before.Status;
        credential.VerifiedAt = before.VerifiedAt;
        credential.ExpiresAt = before.ExpiresAt;
        credential.ReviewedAt = before.ReviewedAt;
        credential.ReviewedByUserId = before.ReviewedByUserId;
        credential.RejectionReason = before.RejectionReason;
        credential.EndedAt = before.EndedAt;
        credential.EndedByUserId = before.EndedByUserId;
        credential.EndReason = before.EndReason;
    }

    private static Result Invalid(ProfessionalCredential credential, CredentialStatus target) =>
        Result.Failure(CredentialErrors.InvalidTransition(credential.Status.ToString(), target.ToString()));
}
