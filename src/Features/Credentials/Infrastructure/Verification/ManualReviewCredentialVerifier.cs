namespace ShapeUp.Features.Credentials.Infrastructure.Verification;

using Shared.Entities;
using Shared.Verification;

/// <summary>
/// Sends every CREF/CRN submission to the admin review queue. It does not query the councils'
/// public pages: there is no documented API, and scraping needs the terms of use checked first.
/// </summary>
public class ManualReviewCredentialVerifier : ICredentialVerifier
{
    public bool Supports(string issuingAuthority) =>
        issuingAuthority is CredentialAuthorities.Cref or CredentialAuthorities.Crn;

    public Task<CredentialVerificationResult> VerifyAsync(ProfessionalCredential credential, CancellationToken cancellationToken) =>
        Task.FromResult(CredentialVerificationResult.ForManualReview());
}
