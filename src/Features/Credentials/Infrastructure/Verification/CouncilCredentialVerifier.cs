namespace ShapeUp.Features.Credentials.Infrastructure.Verification;

using Microsoft.Extensions.Options;
using Shared.Entities;
using Shared.Verification;

/// <summary>
/// Automatic verification for one council: only a strong match is approved, a registration the council does not
/// know is rejected (when <see cref="CouncilEndpointOptions.RejectWhenNotFound"/>), everything else, including a
/// council outage, stays in manual review. Registered before <see cref="ManualReviewCredentialVerifier"/>.
/// </summary>
public abstract class CouncilCredentialVerifier(
    string authority,
    ICouncilRegistryChecker checker,
    IOptions<CouncilVerificationOptions> options) : ICredentialVerifier
{
    public bool Supports(string issuingAuthority) =>
        issuingAuthority == authority && checker.IsEnabled(authority);

    public async Task<CredentialVerificationResult> VerifyAsync(ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        var check = await checker.CheckAsync(credential, cancellationToken);
        return check.Outcome switch
        {
            CouncilCheckOutcome.Verified => new CredentialVerificationResult(CredentialVerificationOutcome.Verified, check.Reason),
            CouncilCheckOutcome.NotFound when options.Value.For(authority).RejectWhenNotFound =>
                new CredentialVerificationResult(CredentialVerificationOutcome.Rejected, check.Reason),
            _ => CredentialVerificationResult.ForManualReview()
        };
    }
}

public sealed class CrefCredentialVerifier(ICouncilRegistryChecker checker, IOptions<CouncilVerificationOptions> options)
    : CouncilCredentialVerifier(CredentialAuthorities.Cref, checker, options);

public sealed class CrnCredentialVerifier(ICouncilRegistryChecker checker, IOptions<CouncilVerificationOptions> options)
    : CouncilCredentialVerifier(CredentialAuthorities.Crn, checker, options);
