using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ShapeUp.Features.Authorization.Resolver;

namespace UnitTests.Domains.Authorization.Resolver;

public class CapabilityPolicyProviderTests
{
    private readonly CapabilityPolicyProvider _provider = new(Options.Create(new AuthorizationOptions()));

    [Fact]
    public async Task GetPolicyAsync_CapabilityPrefixedName_BuildsRequirementFromSuffix()
    {
        var policy = await _provider.GetPolicyAsync("capability:gym.staff.manage");

        Assert.NotNull(policy);
        var requirement = Assert.Single(policy.Requirements.OfType<CapabilityRequirement>());
        Assert.Equal("gym.staff.manage", requirement.Capability);
    }

    [Fact]
    public async Task GetPolicyAsync_NonCapabilityName_FallsBackToDefaultProvider()
    {
        var policy = await _provider.GetPolicyAsync("SomeOtherPolicy");

        Assert.Null(policy);
    }
}
