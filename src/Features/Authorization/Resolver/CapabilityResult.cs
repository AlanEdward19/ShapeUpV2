namespace ShapeUp.Features.Authorization.Resolver;

public sealed record CapabilityResult(bool IsAllowed, string? DenyReason)
{
    public static CapabilityResult Allow() => new(true, null);
    public static CapabilityResult Deny(string reason) => new(false, reason);
}
