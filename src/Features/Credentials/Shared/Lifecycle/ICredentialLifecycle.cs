namespace ShapeUp.Features.Credentials.Shared.Lifecycle;

using Entities;
using ShapeUp.Shared.Results;

/// <summary>The only place a credential changes status, so the state machine and the role stay together.</summary>
public interface ICredentialLifecycle
{
    Task<Result> SendToReviewAsync(ProfessionalCredential credential, CancellationToken cancellationToken);

    /// <summary>UnderReview to Verified, then grants the professional role. <paramref name="reviewerUserId"/> is null for automatic verification.</summary>
    Task<Result> ApproveAsync(ProfessionalCredential credential, int? reviewerUserId, CancellationToken cancellationToken);

    Task<Result> RejectAsync(ProfessionalCredential credential, string reason, int? reviewerUserId, CancellationToken cancellationToken);

    /// <summary>Verified to Expired, Suspended or Revoked, then withdraws the role this credential granted.</summary>
    Task<Result> EndAsync(ProfessionalCredential credential, CredentialStatus target, DateTime nowUtc, CancellationToken cancellationToken);
}
