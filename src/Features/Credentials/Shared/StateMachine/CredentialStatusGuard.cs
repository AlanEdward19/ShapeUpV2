namespace ShapeUp.Features.Credentials.Shared.StateMachine;

using Entities;

/// <summary>
/// Enforces the ProfessionalCredential state machine (spec.md AUTHZ-12):
/// DRAFT→SUBMITTED→UNDER_REVIEW→{VERIFIED,REJECTED}, VERIFIED→{EXPIRED,SUSPENDED,REVOKED}.
/// <see cref="LosesProfessionalAccess"/> marks the end states that withdraw credential-granted roles.
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

    /// <summary>Expired, Suspended and Revoked take back any role the credential granted.</summary>
    public static bool LosesProfessionalAccess(CredentialStatus status) =>
        status is CredentialStatus.Expired or CredentialStatus.Suspended or CredentialStatus.Revoked;
}
