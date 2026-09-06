namespace ShapeUp.Features.Entitlements.Infrastructure;

using Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

public class EntitlementAdapter(
    IUserPlatformRoleRepository userPlatformRoleRepository,
    IPlatformTierRepository platformTierRepository) : IEntitlementRepository
{
    private const string FreeTierName = "Free";

    // SPEC_DEVIATION: PlatformTier has no "granted capabilities" column today (only
    // Name/Price/MaxClients/MaxTrainers -- gym business limits, not consumer entitlements).
    // Mapped here as a code constant until Fase 5 (Monetização) introduces a real model.
    private static readonly IReadOnlySet<string> FreeTierCapabilities = new HashSet<string>();
    private static readonly IReadOnlySet<string> PaidTierCapabilities = new HashSet<string> { "advancedMetrics" };

    public async Task<Entitlement> GetEntitlementAsync(int userId, CancellationToken cancellationToken)
    {
        var roles = await userPlatformRoleRepository.GetByUserIdAsync(userId, cancellationToken);

        PlatformTier? bestTier = null;
        foreach (var role in roles)
        {
            if (!role.IsActive || role.PlatformTierId is null)
                continue;

            var tier = await platformTierRepository.GetByIdAsync(role.PlatformTierId.Value, cancellationToken);
            if (tier is null || !tier.IsActive)
                continue;

            if (bestTier is null || tier.Price > bestTier.Price)
                bestTier = tier;
        }

        if (bestTier is null)
            return new Entitlement(userId, FreeTierName, FreeTierCapabilities);

        return new Entitlement(userId, bestTier.Name, PaidTierCapabilities);
    }
}
