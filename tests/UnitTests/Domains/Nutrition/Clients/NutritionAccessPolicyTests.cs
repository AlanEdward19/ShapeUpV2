using Moq;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;

namespace UnitTests.Domains.Nutrition.Clients;

public class NutritionAccessPolicyTests
{
    private readonly Mock<IProfessionalCapabilityService> _capabilities = new();
    private readonly Mock<IProfessionalClientRelationshipRepository> _relationships = new();
    private readonly NutritionAccessPolicy _policy;

    public NutritionAccessPolicyTests()
    {
        _policy = new NutritionAccessPolicy(_capabilities.Object, _relationships.Object);
    }

    private void SetCapability(bool nutrition) =>
        _capabilities.Setup(c => c.GetAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfessionalCapabilitiesResponse(false, nutrition));

    private void SetRelationship(bool active) =>
        _relationships.Setup(r => r.GetActiveAsync(1, 2, "Nutrition", It.IsAny<CancellationToken>()))
            .ReturnsAsync(active
                ? new ProfessionalClientRelationship { ProfessionalUserId = 1, ClientUserId = 2, RelationshipType = "Nutrition", StartedAt = DateTime.UtcNow }
                : null);

    [Fact]
    public async Task Self_IsAllowedWithoutAnyCapability()
    {
        Assert.True(await _policy.CanManageNutritionForAsync(1, 1, CancellationToken.None));
        _capabilities.Verify(c => c.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithoutCapability_IsDeniedEvenWithRelationship()
    {
        SetCapability(false);
        SetRelationship(true);

        Assert.False(await _policy.CanManageNutritionForAsync(1, 2, CancellationToken.None));
    }

    [Fact]
    public async Task WithCapabilityButNoRelationship_IsDenied()
    {
        SetCapability(true);
        SetRelationship(false);

        Assert.False(await _policy.CanManageNutritionForAsync(1, 2, CancellationToken.None));
    }

    [Fact]
    public async Task WithCapabilityAndActiveRelationship_IsAllowed()
    {
        SetCapability(true);
        SetRelationship(true);

        Assert.True(await _policy.CanManageNutritionForAsync(1, 2, CancellationToken.None));
    }

    [Fact]
    public async Task TrainingRelationshipDoesNotGrantNutritionAccess()
    {
        SetCapability(true);
        _relationships.Setup(r => r.GetActiveAsync(1, 2, "Training", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfessionalClientRelationship { ProfessionalUserId = 1, ClientUserId = 2, RelationshipType = "Training", StartedAt = DateTime.UtcNow });

        Assert.False(await _policy.CanManageNutritionForAsync(1, 2, CancellationToken.None));
    }
}
