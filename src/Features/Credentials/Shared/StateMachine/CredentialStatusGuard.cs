namespace ShapeUp.Features.Credentials.Shared.StateMachine;

using Entities;

/// <summary>
/// Enforces the ProfessionalCredential state machine (spec.md AUTHZ-12):
/// DRAFT→SUBMITTED→UNDER_REVIEW→{VERIFIED,REJECTED}, VERIFIED→{EXPIRED,SUSPENDED,REVOKED}.
/// </summary>
public static class CredentialStatusGuard
{
    private static readonly Dictionary<CredentialStatus, CredentialStatus[]> AllowedTransitions = new()
    {
        [CredentialStatus.Draft] = [CredentialStatus.Submitted],
        [CredentialStatus.Submitted] = [CredentialStatus.UnderReview],
        [CredentialStatus.UnderReview] = [CredentialStatus.Verified, CredentialStatus.Rejected],
        [CredentialStatus.Verified] = [CredentialStatus.Expired, CredentialStatus.Suspended, CredentialStatus.Revoked],
        [CredentialStatus.Rejected] = [],
        [CredentialStatus.Expired] = [],
        [CredentialStatus.Suspended] = [],
        [CredentialStatus.Revoked] = []
    };

    public static bool IsValidTransition(CredentialStatus from, CredentialStatus to)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }
}
