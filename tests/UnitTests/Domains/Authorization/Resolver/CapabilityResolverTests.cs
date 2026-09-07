using Moq;
using ShapeUp.Features.Authorization.Resolver;
using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.Entitlements.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Memberships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;

namespace UnitTests.Domains.Authorization.Resolver;

public class CapabilityResolverTests
{
    private readonly Mock<IOrganizationMembershipRepository> _membershipRepository = new();
    private readonly Mock<IProfessionalCredentialRepository> _credentialRepository = new();
    private readonly Mock<IProfessionalClientRelationshipRepository> _relationshipRepository = new();
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();
    private readonly Mock<IUserPlatformRoleRepository> _userPlatformRoleRepository = new();
    private readonly CapabilityResolver _resolver;

    public CapabilityResolverTests()
    {
        _resolver = new CapabilityResolver(
            _membershipRepository.Object,
            _credentialRepository.Object,
            _relationshipRepository.Object,
            _entitlementRepository.Object,
            _userPlatformRoleRepository.Object);
    }

    [Fact]
    public async Task ResolveAsync_RequiresPlatformAdmin_UserIsActiveAdmin_Allows()
    {
        _userPlatformRoleRepository.Setup(r => r.GetByUserIdAndRoleAsync(1, PlatformRoleType.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPlatformRole { UserId = 1, Role = PlatformRoleType.Admin, IsActive = true });

        var result = await _resolver.ResolveAsync(1, "platform.exercises.manage", new AuthorizationContext(RequiresPlatformAdmin: true), CancellationToken.None);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_RequiresPlatformAdmin_UserIsNotAdmin_Denies()
    {
        _userPlatformRoleRepository.Setup(r => r.GetByUserIdAndRoleAsync(1, PlatformRoleType.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPlatformRole?)null);

        var result = await _resolver.ResolveAsync(1, "platform.exercises.manage", new AuthorizationContext(RequiresPlatformAdmin: true), CancellationToken.None);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_RequiresPlatformAdmin_AdminRoleInactive_Denies()
    {
        _userPlatformRoleRepository.Setup(r => r.GetByUserIdAndRoleAsync(1, PlatformRoleType.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPlatformRole { UserId = 1, Role = PlatformRoleType.Admin, IsActive = false });

        var result = await _resolver.ResolveAsync(1, "platform.exercises.manage", new AuthorizationContext(RequiresPlatformAdmin: true), CancellationToken.None);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_RequiresPlatformAdmin_IgnoresSelfAccess_DeniesEvenWhenTargetIsSelf()
    {
        _userPlatformRoleRepository.Setup(r => r.GetByUserIdAndRoleAsync(1, PlatformRoleType.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPlatformRole?)null);

        var result = await _resolver.ResolveAsync(1, "platform.exercises.manage", new AuthorizationContext(TargetUserId: 1, RequiresPlatformAdmin: true), CancellationToken.None);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_MembershipExists_Allows()
    {
        _membershipRepository.Setup(r => r.GetMembershipAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationMembership(1, 10, MembershipRole.Owner));

        var result = await _resolver.ResolveAsync(1, "gym.staff.manage", new AuthorizationContext(GymId: 10), CancellationToken.None);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_NoMembership_Denies()
    {
        _membershipRepository.Setup(r => r.GetMembershipAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationMembership?)null);

        var result = await _resolver.ResolveAsync(1, "gym.staff.manage", new AuthorizationContext(GymId: 10), CancellationToken.None);

        Assert.False(result.IsAllowed);
        Assert.NotNull(result.DenyReason);
    }

    [Fact]
    public async Task ResolveAsync_ActiveRelationshipExists_Allows()
    {
        _relationshipRepository.Setup(r => r.GetActiveAsync(1, 2, "Training", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfessionalClientRelationship { ProfessionalUserId = 1, ClientUserId = 2, RelationshipType = "Training", StartedAt = DateTime.UtcNow });

        var result = await _resolver.ResolveAsync(1, "training.workouts.read", new AuthorizationContext(TargetUserId: 2, RelationshipType: "Training"), CancellationToken.None);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_NoActiveRelationship_Denies()
    {
        _relationshipRepository.Setup(r => r.GetActiveAsync(1, 2, "Training", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfessionalClientRelationship?)null);

        var result = await _resolver.ResolveAsync(1, "training.workouts.read", new AuthorizationContext(TargetUserId: 2, RelationshipType: "Training"), CancellationToken.None);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_VerifiedCredentialExists_Allows()
    {
        _credentialRepository.Setup(r => r.GetVerifiedAsync(1, "Nutritionist", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfessionalCredential
            {
                UserId = 1,
                ProfessionType = "Nutritionist",
                CredentialNumber = "CRN-1",
                IssuingAuthority = "CRN",
                IssuingRegion = "SP",
                Country = "BR",
                Status = CredentialStatus.Verified
            });

        var result = await _resolver.ResolveAsync(1, "nutrition.plans.create", new AuthorizationContext(RequiredProfessionType: "Nutritionist"), CancellationToken.None);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_NoVerifiedCredential_Denies()
    {
        _credentialRepository.Setup(r => r.GetVerifiedAsync(1, "Nutritionist", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfessionalCredential?)null);

        var result = await _resolver.ResolveAsync(1, "nutrition.plans.create", new AuthorizationContext(RequiredProfessionType: "Nutritionist"), CancellationToken.None);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_EntitlementGrantsCapability_Allows()
    {
        _entitlementRepository.Setup(r => r.GetEntitlementAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Entitlement(1, "Pro", new HashSet<string> { "advancedMetrics" }));

        var result = await _resolver.ResolveAsync(1, "analytics.advanced.view", new AuthorizationContext(RequiredEntitlementCapability: "advancedMetrics"), CancellationToken.None);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_EntitlementDoesNotGrantCapability_Denies()
    {
        _entitlementRepository.Setup(r => r.GetEntitlementAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Entitlement(1, "Free", new HashSet<string>()));

        var result = await _resolver.ResolveAsync(1, "analytics.advanced.view", new AuthorizationContext(RequiredEntitlementCapability: "advancedMetrics"), CancellationToken.None);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_TargetUserIsSelf_AllowsWithoutConsultingOtherSources()
    {
        var result = await _resolver.ResolveAsync(1, "training.workouts.read", new AuthorizationContext(TargetUserId: 1), CancellationToken.None);

        Assert.True(result.IsAllowed);
        _membershipRepository.VerifyNoOtherCalls();
        _relationshipRepository.VerifyNoOtherCalls();
        _credentialRepository.VerifyNoOtherCalls();
        _entitlementRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_NoContextPopulatedAtAll_Denies()
    {
        var result = await _resolver.ResolveAsync(1, "some.capability", new AuthorizationContext(), CancellationToken.None);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task ResolveAsync_MembershipRepositoryThrows_PropagatesException()
    {
        _membershipRepository.Setup(r => r.GetMembershipAsync(1, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db unavailable"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _resolver.ResolveAsync(1, "gym.staff.manage", new AuthorizationContext(GymId: 10), CancellationToken.None));
    }
}
