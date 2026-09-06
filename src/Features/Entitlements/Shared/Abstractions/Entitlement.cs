namespace ShapeUp.Features.Entitlements.Shared.Abstractions;

public record Entitlement(int UserId, string TierName, IReadOnlySet<string> GrantedCapabilities);
