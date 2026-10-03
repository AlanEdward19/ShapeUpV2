using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace UnitTests.Domains.Authorization;

public class GetProfessionalCapabilitiesHandlerTests
{
    private readonly Mock<IUserPlatformRoleRepository> _roles = new();
    private readonly Mock<IProfessionalCredentialRepository> _credentials = new();

    private GetProfessionalCapabilitiesHandler Handler() => new(_roles.Object, _credentials.Object);

    [Fact]
    public async Task HandleAsync_ActiveTrainerRole_AllowsTrainingOnly()
    {
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(5, PlatformRoleType.Trainer, default))
            .ReturnsAsync(new UserPlatformRole { UserId = 5, Role = PlatformRoleType.Trainer, IsActive = true });

        var result = await Handler().HandleAsync(5, default);

        Assert.True(result.Value!.Training);
        Assert.False(result.Value.Nutrition);
    }

    [Fact]
    public async Task HandleAsync_VerifiedNutritionistCredential_AllowsNutrition()
    {
        _credentials.Setup(c => c.GetVerifiedAsync(5, "Nutritionist", It.IsAny<DateTime>(), default))
            .ReturnsAsync(new ProfessionalCredential { UserId = 5, ProfessionType = "Nutritionist", CredentialNumber = "1", IssuingAuthority = "CRN", IssuingRegion = "SP", Country = "BR", Status = CredentialStatus.Verified });

        var result = await Handler().HandleAsync(5, default);

        Assert.False(result.Value!.Training);
        Assert.True(result.Value.Nutrition);
    }

    [Fact]
    public async Task HandleAsync_PlainClient_AllowsNothing()
    {
        var result = await Handler().HandleAsync(5, default);

        Assert.False(result.Value!.Training);
        Assert.False(result.Value.Nutrition);
    }
}
