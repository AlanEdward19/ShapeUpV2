namespace ShapeUp.Features.Authorization.Resolver;

using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.Entitlements.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Memberships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Abstractions;

/// <summary>
/// Combines Identity + Membership + Credentials + Relationships + Entitlements into a single
/// allow/deny decision (RFC-001). Only consults the source relevant to whichever
/// <see cref="AuthorizationContext"/> field is populated by the caller -- it never invents a
/// capability-to-role table that has no basis in spec.md/design.md. Self-access (the user
/// acting on their own data) always allows, independent of the other sources (AUTHZ-10).
/// </summary>
public class CapabilityResolver(
    IOrganizationMembershipRepository membershipRepository,
    IProfessionalCredentialRepository credentialRepository,
    IProfessionalClientRelationshipRepository relationshipRepository,
    IEntitlementRepository entitlementRepository,
    IUserPlatformRoleRepository userPlatformRoleRepository) : ICapabilityResolver
{
    public async Task<CapabilityResult> ResolveAsync(
        int userId,
        string capability,
        AuthorizationContext context,
        CancellationToken cancellationToken)
    {
        if (context.RequiresPlatformAdmin)
        {
            var adminRole = await userPlatformRoleRepository.GetByUserIdAndRoleAsync(userId, PlatformRoleType.Admin, cancellationToken);
            if (adminRole is not null && adminRole.IsActive)
                return CapabilityResult.Allow();

            return CapabilityResult.Deny($"User {userId} is not a platform admin, required for capability '{capability}'.");
        }

        if (context.TargetUserId == userId)
            return CapabilityResult.Allow();

        if (context.GymId is { } gymId)
        {
            var membership = await membershipRepository.GetMembershipAsync(userId, gymId, cancellationToken);
            if (membership is not null)
                return CapabilityResult.Allow();
        }

        if (context is { TargetUserId: { } targetUserId, RelationshipType: { } relationshipType })
        {
            var relationship = await relationshipRepository.GetActiveAsync(userId, targetUserId, relationshipType, cancellationToken);
            if (relationship is not null)
                return CapabilityResult.Allow();
        }

        if (context.RequiredProfessionType is { } professionType)
        {
            var credential = await credentialRepository.GetVerifiedAsync(userId, professionType, DateTime.UtcNow, cancellationToken);
            if (credential is not null)
                return CapabilityResult.Allow();
        }

        if (context.RequiredEntitlementCapability is { } requiredCapability)
        {
            var entitlement = await entitlementRepository.GetEntitlementAsync(userId, cancellationToken);
            if (entitlement.GrantedCapabilities.Contains(requiredCapability))
                return CapabilityResult.Allow();
        }

        return CapabilityResult.Deny($"No source granted capability '{capability}' to user {userId}.");
    }
}
