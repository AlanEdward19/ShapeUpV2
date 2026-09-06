using Moq;
using ShapeUp.Features.Entitlements.Infrastructure;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace UnitTests.Domains.Entitlements;

public class EntitlementAdapterTests
{
    private readonly Mock<IUserPlatformRoleRepository> _userPlatformRoleRepository = new();
    private readonly Mock<IPlatformTierRepository> _platformTierRepository = new();
    private readonly EntitlementAdapter _adapter;

    public EntitlementAdapterTests()
    {
        _adapter = new EntitlementAdapter(_userPlatformRoleRepository.Object, _platformTierRepository.Object);
    }

    [Fact]
    public async Task GetEntitlementAsync_NoPlatformTierAssigned_ReturnsFreeWithNoCapabilities()
    {
        _userPlatformRoleRepository.Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserPlatformRole { UserId = 1, Role = PlatformRoleType.IndependentClient, PlatformTierId = null, IsActive = true }]);

        var result = await _adapter.GetEntitlementAsync(1, CancellationToken.None);

        Assert.Equal("Free", result.TierName);
        Assert.Empty(result.GrantedCapabilities);
    }

    [Fact]
    public async Task GetEntitlementAsync_ActiveTierAssigned_ReturnsTierWithCapabilities()
    {
        _userPlatformRoleRepository.Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserPlatformRole { UserId = 1, Role = PlatformRoleType.IndependentClient, PlatformTierId = 5, IsActive = true }]);
        _platformTierRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformTier { Id = 5, Name = "Pro", Price = 79.90m, IsActive = true });

        var result = await _adapter.GetEntitlementAsync(1, CancellationToken.None);

        Assert.Equal("Pro", result.TierName);
        Assert.Contains("advancedMetrics", result.GrantedCapabilities);
    }

    [Fact]
    public async Task GetEntitlementAsync_TierExistsButIsInactive_FallsBackToFree()
    {
        _userPlatformRoleRepository.Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserPlatformRole { UserId = 1, Role = PlatformRoleType.IndependentClient, PlatformTierId = 5, IsActive = true }]);
        _platformTierRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformTier { Id = 5, Name = "Pro", Price = 79.90m, IsActive = false });

        var result = await _adapter.GetEntitlementAsync(1, CancellationToken.None);

        Assert.Equal("Free", result.TierName);
    }
}
