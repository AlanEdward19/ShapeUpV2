namespace ShapeUp.Features.Authorization.Resolver;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

/// <summary>
/// Dynamic policy provider (AD-001): any policy name prefixed "capability:" is built on the fly
/// as a CapabilityRequirement -- no AddPolicy call needed per capability. This lets controller
/// migrations (T8-T24) touch only their own file, with no shared DI registration to edit (and
/// no merge conflict when migrated in parallel).
/// </summary>
public class CapabilityPolicyProvider : IAuthorizationPolicyProvider
{
    private const string CapabilityPrefix = "capability:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public CapabilityPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(CapabilityPrefix, StringComparison.Ordinal))
        {
            var capability = policyName[CapabilityPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new CapabilityRequirement(capability))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
