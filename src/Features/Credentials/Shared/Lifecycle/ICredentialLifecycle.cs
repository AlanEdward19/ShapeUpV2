namespace ShapeUp.Features.Credentials.Shared.Lifecycle;

using Entities;
using ShapeUp.Shared.Results;

/// <summary>The only place a credential changes status, so the state machine and the role stay together.</summary>
public interface ICredentialLifecycle
{
    Task<Result> SendToReviewAsync(ProfessionalCredential credential, CancellationToken cancellationToken);

    /// <summary>
    /// UnderReview to Verified, then grants the professional role. <paramref name="reviewerUserId"/> is null for automatic verification.
    /// <paramref name="expiresAtUtc"/> defaults to 12 months from now.
    /// </summary>
    Task<Result> ApproveAsync(
        ProfessionalCredential credential, int? reviewerUserId, CancellationToken cancellationToken, DateTime? expiresAtUtc = null);

    Task<Result> RejectAsync(ProfessionalCredential credential, string reason, int? reviewerUserId, CancellationToken cancellationToken);

    /// <summary>UnderReview to NeedsChanges: asks the professional to correct and resubmit the same credential.</summary>
    Task<Result> RequestChangesAsync(
        ProfessionalCredential credential, string message, int? reviewerUserId, CancellationToken cancellationToken);

    /// <summary>NeedsChanges to Submitted with the corrected registration data; the caller then runs verification again.</summary>
    Task<Result> ResubmitAsync(
        ProfessionalCredential credential, string credentialNumber, string issuingRegion, CancellationToken cancellationToken);

    /// <summary>Verified to Expired, Suspended or Revoked, then withdraws the role this credential granted.</summary>
    Task<Result> EndAsync(
        ProfessionalCredential credential,
        CredentialStatus target,
        DateTime nowUtc,
        CancellationToken cancellationToken,
        string? reason = null,
        int? endedByUserId = null);
}
