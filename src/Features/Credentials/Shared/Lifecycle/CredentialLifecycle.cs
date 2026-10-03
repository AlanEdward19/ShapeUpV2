namespace ShapeUp.Features.Credentials.Shared.Lifecycle;

using Abstractions;
using Entities;
using Errors;
using StateMachine;
using ShapeUp.Shared.Results;

public class CredentialLifecycle(
    IProfessionalCredentialRepository repository,
    IProfessionalRoleGranter roleGranter) : ICredentialLifecycle
{
    public async Task<Result> SendToReviewAsync(ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        if (!CredentialStatusGuard.IsValidTransition(credential.Status, CredentialStatus.UnderReview))
            return Invalid(credential, CredentialStatus.UnderReview);

        credential.Status = CredentialStatus.UnderReview;
        await repository.UpdateAsync(credential, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ApproveAsync(ProfessionalCredential credential, int? reviewerUserId, CancellationToken cancellationToken)
    {
        if (!CredentialStatusGuard.IsValidTransition(credential.Status, CredentialStatus.Verified))
            return Invalid(credential, CredentialStatus.Verified);

        var now = DateTime.UtcNow;
        credential.Status = CredentialStatus.Verified;
        credential.VerifiedAt = now;
        credential.ReviewedAt = now;
        credential.ReviewedByUserId = reviewerUserId;
        credential.RejectionReason = null;
        await repository.UpdateAsync(credential, cancellationToken);

        await roleGranter.GrantAsync(credential, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RejectAsync(ProfessionalCredential credential, string reason, int? reviewerUserId, CancellationToken cancellationToken)
    {
        if (!CredentialStatusGuard.IsValidTransition(credential.Status, CredentialStatus.Rejected))
            return Invalid(credential, CredentialStatus.Rejected);

        credential.Status = CredentialStatus.Rejected;
        credential.ReviewedAt = DateTime.UtcNow;
        credential.ReviewedByUserId = reviewerUserId;
        credential.RejectionReason = reason;
        await repository.UpdateAsync(credential, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> EndAsync(ProfessionalCredential credential, CredentialStatus target, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (!CredentialStatusGuard.LosesProfessionalAccess(target)
            || !CredentialStatusGuard.IsValidTransition(credential.Status, target))
            return Invalid(credential, target);

        credential.Status = target;
        await repository.UpdateAsync(credential, cancellationToken);

        await roleGranter.ReleaseAsync(credential, nowUtc, cancellationToken);
        return Result.Success();
    }

    private static Result Invalid(ProfessionalCredential credential, CredentialStatus target) =>
        Result.Failure(CredentialErrors.InvalidTransition(credential.Status.ToString(), target.ToString()));
}
