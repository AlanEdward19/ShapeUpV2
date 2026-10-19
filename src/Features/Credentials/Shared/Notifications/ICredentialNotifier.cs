namespace ShapeUp.Features.Credentials.Shared.Notifications;

using Entities;

/// <summary>
/// Tells the professional about each step of their registration. Implementations never throw: a delivery
/// failure is logged and retried, and must not undo the status change that triggered it.
/// </summary>
public interface ICredentialNotifier
{
    /// <summary>The registration is waiting for a reviewer (status UnderReview).</summary>
    Task ReceivedAsync(ProfessionalCredential credential, CancellationToken cancellationToken);

    /// <summary>Verified: the professional role was granted.</summary>
    Task ApprovedAsync(ProfessionalCredential credential, CancellationToken cancellationToken);

    /// <summary>Rejected, with <see cref="ProfessionalCredential.RejectionReason"/>.</summary>
    Task RejectedAsync(ProfessionalCredential credential, CancellationToken cancellationToken);

    /// <summary>The reviewer asked for a correction (<see cref="ProfessionalCredential.ChangesRequestedMessage"/>).</summary>
    Task ChangesRequestedAsync(ProfessionalCredential credential, CancellationToken cancellationToken);

    /// <summary>Expired, Suspended or Revoked: the professional access was withdrawn.</summary>
    Task AccessEndedAsync(ProfessionalCredential credential, CancellationToken cancellationToken);
}
