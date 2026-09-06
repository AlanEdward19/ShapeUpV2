namespace ShapeUp.Features.Authorization.Resolver;

using Microsoft.AspNetCore.Authorization;

public sealed class CapabilityRequirement(string capability) : IAuthorizationRequirement
{
    public string Capability { get; } = capability;
}
