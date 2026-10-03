namespace ShapeUp.Features.Credentials.Shared.Verification;

using Entities;

public enum CredentialVerificationOutcome
{
    /// <summary>The verifier could not decide on its own; a person has to look at it.</summary>
    UnderReview,
    Verified,
    Rejected
}

public record CredentialVerificationResult(CredentialVerificationOutcome Outcome, string? Reason = null)
{
    public static CredentialVerificationResult ForManualReview() => new(CredentialVerificationOutcome.UnderReview);
}

/// <summary>
/// Checks a submitted credential against its council (CREF for trainers, CRN for nutritionists).
/// Today only <c>ManualReviewCredentialVerifier</c> exists, because neither council publishes an API;
/// an automatic verifier for one council is added as another implementation that supports it.
/// </summary>
public interface ICredentialVerifier
{
    bool Supports(string issuingAuthority);

    Task<CredentialVerificationResult> VerifyAsync(ProfessionalCredential credential, CancellationToken cancellationToken);
}
