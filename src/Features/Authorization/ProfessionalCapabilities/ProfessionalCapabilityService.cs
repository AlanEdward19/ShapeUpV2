using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace ShapeUp.Features.Authorization.ProfessionalCapabilities;

/// <summary>
/// Independent per feature, so one person can be trainer, nutritionist or both.
/// Training: active Trainer platform role or a verified "PersonalTrainer" credential.
/// Nutrition: active Nutritionist platform role or a verified "Nutritionist" credential.
/// </summary>
public class ProfessionalCapabilityService(
    IUserPlatformRoleRepository roleRepository,
    IProfessionalCredentialRepository credentialRepository) : IProfessionalCapabilityService
{
    public const string PersonalTrainerProfession = "PersonalTrainer";
    public const string NutritionistProfession = "Nutritionist";

    public async Task<ProfessionalCapabilitiesResponse> GetAsync(int userId, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var trainerRole = await roleRepository.GetByUserIdAndRoleAsync(userId, PlatformRoleType.Trainer, cancellationToken);
        var training = trainerRole is { IsActive: true }
            || await credentialRepository.GetVerifiedAsync(userId, PersonalTrainerProfession, nowUtc, cancellationToken) is not null;

        var nutritionistRole = await roleRepository.GetByUserIdAndRoleAsync(userId, PlatformRoleType.Nutritionist, cancellationToken);
        var nutrition = nutritionistRole is { IsActive: true }
            || await credentialRepository.GetVerifiedAsync(userId, NutritionistProfession, nowUtc, cancellationToken) is not null;

        return new ProfessionalCapabilitiesResponse(training, nutrition);
    }
}
