namespace ShapeUp.Features.Entitlements.Shared.Abstractions;

public interface IEntitlementRepository
{
    /// <summary>
    /// Returns the user's effective entitlement. Falls back to the Free tier when the user
    /// has no active PlatformTier assignment.
    /// </summary>
    Task<Entitlement> GetEntitlementAsync(int userId, CancellationToken cancellationToken);
}
