namespace ShapeUp.Features.Credentials.Shared.Lifecycle;

using Entities;
using Notifications;
using Verification;
using ShapeUp.Shared.Results;

/// <summary>
/// What happens after a registration is submitted (or resubmitted): the first verifier that supports the council
/// decides, the credential goes to review, and an automatic Verified/Rejected is applied on top of that.
/// Whatever stays in review gets the "received" e-mail; a decision gets its own e-mail from the lifecycle.
/// </summary>
public static class CredentialVerificationFlow
{
    public static async Task<Result> RunAsync(
        ProfessionalCredential credential,
        IEnumerable<ICredentialVerifier> verifiers,
        ICredentialLifecycle lifecycle,
        ICredentialNotifier? notifier,
        CancellationToken cancellationToken)
    {
        var verifier = verifiers.FirstOrDefault(v => v.Supports(credential.IssuingAuthority));
        var verification = verifier is null
            ? CredentialVerificationResult.ForManualReview()
            : await verifier.VerifyAsync(credential, cancellationToken);

        // Submitted -> UnderReview always comes first; the state machine has no shortcut to Verified/Rejected.
        var review = await lifecycle.SendToReviewAsync(credential, cancellationToken);
        if (review.IsFailure)
            return review;

        var decision = verification.Outcome switch
        {
            CredentialVerificationOutcome.Verified => await lifecycle.ApproveAsync(credential, null, cancellationToken),
            CredentialVerificationOutcome.Rejected => await lifecycle.RejectAsync(
                credential, verification.Reason ?? "Registration not confirmed by the council.", null, cancellationToken),
            _ => Result.Success()
        };
        if (decision.IsFailure)
            return decision;

        if (credential.Status == CredentialStatus.UnderReview && notifier is not null)
            await notifier.ReceivedAsync(credential, cancellationToken);
        return Result.Success();
    }
}
